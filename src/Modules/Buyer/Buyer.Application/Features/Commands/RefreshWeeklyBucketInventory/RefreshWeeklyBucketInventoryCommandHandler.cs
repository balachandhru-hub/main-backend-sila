using Buyer.Application.Contracts;
using Buyer.Application.Features.Queries.GetWeeklyBucket;
using Buyer.Application.Features.Shared;
using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.RefreshWeeklyBucketInventory
{
    public class RefreshWeeklyBucketInventoryCommandHandler : IRequestHandler<RefreshWeeklyBucketInventoryCommand, WeeklyBucketDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IStockInHandProvider _stockInHandProvider;
        private readonly IMediator _mediator;

        public RefreshWeeklyBucketInventoryCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient,
            IStockInHandProvider stockInHandProvider,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
            _stockInHandProvider = stockInHandProvider;
            _mediator = mediator;
        }

        public async Task<WeeklyBucketDetailDto> Handle(RefreshWeeklyBucketInventoryCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Refreshing weekly bucket inventory. WeeklyBucketId: {request.WeeklyBucketId}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            WeeklyBucketRules.EnsureOpen(bucket, _logger);

            List<WeeklyBucketItem> items = await _repository.WeeklyBucket.GetItemsAsync(bucket.Id, cancellationToken);
            int created = 0;
            if (items.Count > 0)
            {
                // One call for every product of the bucket. A failed call throws, so old numbers are never shown as fresh.
                List<Guid> catalogIds = items.Select(x => x.CatalogId).Distinct().ToList();
                List<BuyerCatalogItemDto> stock = await _supplierApiClient.GetBuyerCatalogStock(catalogIds, cancellationToken);
                List<WeeklyBucketRecommendation> recommendations = await _repository.WeeklyBucket.GetRecommendationsAsync(bucket.Id, cancellationToken);
                int sequence = recommendations.Count == 0 ? 0 : recommendations.Max(x => x.Sequence);
                DateTime refreshedOn = DateTime.UtcNow;

                foreach (WeeklyBucketItem item in items)
                {
                    // A product the supplier no longer lists has no stock figure: the line becomes UNKNOWN.
                    BuyerCatalogItemDto? product = stock.FirstOrDefault(x => x.CatalogId == item.CatalogId);
                    item.SupplierStock = product?.AvailableStock;
                    item.SupplierStockRefreshedOn = refreshedOn;
                    if (product != null)
                    {
                        item.Price = product.Price;
                        item.Currency = product.Currency ?? item.Currency;
                        item.DiscountPercent = product.DiscountPercent;
                    }

                    item.StockInHand = await _stockInHandProvider.GetStockInHandAsync(
                        buyer.Id, item.StorageLocation, item.MaterialCode, cancellationToken);
                    item.AvailabilityStatus = WeeklyBucketRules.GetAvailability(item.SupplierStock, item.ApprovedQuantity);

                    if (!WeeklyBucketRules.IsShort(item.AvailabilityStatus))
                    {
                        continue;
                    }

                    bool hasRecommendation = recommendations.Any(
                        x => x.WeeklyBucketItemId == item.Id
                             && (x.Status == Common.RECOMMENDATION_PENDING || x.Status == Common.RECOMMENDATION_APPROVED));
                    if (hasRecommendation)
                    {
                        continue;
                    }

                    // The first alternative that was not already rejected for this line. No alternative leaves the line as it is.
                    List<Guid> rejectedCatalogIds = recommendations
                        .Where(x => x.WeeklyBucketItemId == item.Id && x.Status == Common.RECOMMENDATION_REJECTED)
                        .Select(x => x.CatalogId)
                        .ToList();
                    List<BuyerCatalogItemDto> alternatives = await _supplierApiClient.GetBuyerCatalogAlternatives(
                        item.CatalogId, item.ApprovedQuantity, cancellationToken);
                    BuyerCatalogItemDto? alternative = alternatives.FirstOrDefault(
                        x => x.CatalogId != null && !rejectedCatalogIds.Contains(x.CatalogId.Value));
                    if (alternative == null)
                    {
                        continue;
                    }

                    sequence += 1;
                    WeeklyBucketRecommendation recommendation = new WeeklyBucketRecommendation
                    {
                        Id = Guid.NewGuid(),
                        WeeklyBucketId = bucket.Id,
                        WeeklyBucketItemId = item.Id,
                        RecommendationNumber = $"{Common.RECOMMENDATION_NUMBER_PREFIX}{sequence}",
                        Sequence = sequence,
                        CatalogId = alternative.CatalogId!.Value,
                        Sku = alternative.Sku,
                        ProductName = alternative.CatalogName ?? alternative.Description ?? string.Empty,
                        Description = alternative.Description,
                        SupplierId = alternative.SupplierId,
                        SupplierName = alternative.SupplierName,
                        UnitOfMeasure = alternative.UnitOfMeasure,
                        Price = alternative.Price,
                        Currency = alternative.Currency,
                        DiscountPercent = alternative.DiscountPercent,
                        AvailableStock = alternative.AvailableStock,
                        Quantity = item.ApprovedQuantity,
                        Status = Common.RECOMMENDATION_PENDING
                    };
                    _repository.WeeklyBucketRecommendation.Create(recommendation);
                    recommendations.Add(recommendation);
                    item.LineStatus = Common.LINE_RECOMMENDATION_PENDING;
                    created += 1;
                    WeeklyBucketRules.AddAudit(
                        _repository,
                        bucket.Id,
                        request.UserId,
                        Common.AUDIT_RECOMMENDATION_CREATED,
                        $"{recommendation.RecommendationNumber}: {item.ProductName} ({item.AvailabilityStatus}) -> {recommendation.ProductName}");
                }
            }

            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_INVENTORY_REFRESHED,
                $"Lines={items.Count} RecommendationsCreated={created}");
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Weekly bucket inventory refreshed. WeeklyBucketId: {bucket.Id}, Lines: {items.Count}, RecommendationsCreated: {created}");
            return await _mediator.Send(
                new GetWeeklyBucketQuery
                {
                    OrganizationId = request.OrganizationId,
                    WeeklyBucketId = bucket.Id
                },
                cancellationToken);
        }

        private BuyerBusinessProfile GetBuyer(Guid organizationId)
        {
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            return buyer;
        }
    }
}
