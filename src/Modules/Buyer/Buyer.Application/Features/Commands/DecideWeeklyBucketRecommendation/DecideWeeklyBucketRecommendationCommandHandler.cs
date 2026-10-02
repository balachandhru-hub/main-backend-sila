using Buyer.Application.Features.Shared;
using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DecideWeeklyBucketRecommendation
{
    public class DecideWeeklyBucketRecommendationCommandHandler : IRequestHandler<DecideWeeklyBucketRecommendationCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IStockInHandProvider _stockInHandProvider;

        public DecideWeeklyBucketRecommendationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IStockInHandProvider stockInHandProvider)
        {
            _repository = repository;
            _logger = logger;
            _stockInHandProvider = stockInHandProvider;
        }

        public async Task<Unit> Handle(DecideWeeklyBucketRecommendationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Deciding recommendation. WeeklyBucketId: {request.WeeklyBucketId}, RecommendationId: {request.RecommendationId}, UserId: {request.UserId}");

            if (request.Decision == null || string.IsNullOrWhiteSpace(request.Decision.Status))
            {
                _logger.LogError($"Decision status is missing. RecommendationId: {request.RecommendationId}");
                throw new BadRequestCustomException("Decision is required.", "Please provide APPROVE or REJECT.");
            }

            if (request.Decision.Status != Common.APPROVED && request.Decision.Status != Common.REJECTED)
            {
                _logger.LogError($"Decision status is invalid. RecommendationId: {request.RecommendationId}, Status: {request.Decision.Status}");
                throw new BadRequestCustomException("Invalid decision.", "Status must be APPROVE or REJECT.");
            }

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            WeeklyBucketRules.EnsureOpen(bucket, _logger);

            List<WeeklyBucketRecommendation> recommendations = await _repository.WeeklyBucket.GetRecommendationsAsync(bucket.Id, cancellationToken);
            WeeklyBucketRecommendation? recommendation = recommendations.FirstOrDefault(x => x.Id == request.RecommendationId);
            if (recommendation == null)
            {
                _logger.LogError($"Recommendation not found. WeeklyBucketId: {bucket.Id}, RecommendationId: {request.RecommendationId}");
                throw new NotFoundCustomException("Recommendation not found.", "The recommendation does not belong to this weekly bucket.");
            }

            if (recommendation.Status != Common.RECOMMENDATION_PENDING)
            {
                _logger.LogError($"Recommendation is already decided. RecommendationId: {recommendation.Id}, Status: {recommendation.Status}");
                throw new BadRequestCustomException("Recommendation is already decided.", "Only a pending recommendation can be approved or rejected.");
            }

            WeeklyBucketItem? item = await _repository.WeeklyBucket.GetItemAsync(bucket.Id, recommendation.WeeklyBucketItemId, cancellationToken);
            if (item == null)
            {
                _logger.LogError($"Line of the recommendation not found. RecommendationId: {recommendation.Id}, ItemId: {recommendation.WeeklyBucketItemId}");
                throw new NotFoundCustomException("Product not found in the weekly bucket.", "The line of this recommendation no longer exists.");
            }

            // The line's requestor, or a user assigned to the line's outlet.
            bool isRequestor = item.RequestorUserId == request.UserId;
            bool isOutletUser = await _repository.BuyerOutletUserMapping
                .FindByCondition(x => x.UserId == request.UserId && x.OutletId == item.OutletId && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!isRequestor && !isOutletUser)
            {
                _logger.LogError($"User may not decide the recommendation. RecommendationId: {recommendation.Id}, UserId: {request.UserId}");
                throw new ForBiddenCustomException(
                    "You cannot decide this recommendation.",
                    "Only the requestor of the product or a user of its outlet can approve or reject the recommendation.");
            }

            recommendation.DecidedBy = request.UserId;
            recommendation.DecidedOn = DateTime.UtcNow;

            if (request.Decision.Status == Common.REJECTED)
            {
                recommendation.Status = Common.RECOMMENDATION_REJECTED;
                item.LineStatus = Common.LINE_REQUESTED;
                WeeklyBucketRules.AddAudit(
                    _repository,
                    bucket.Id,
                    request.UserId,
                    Common.AUDIT_RECOMMENDATION_REJECTED,
                    $"{recommendation.RecommendationNumber}: {recommendation.ProductName} rejected for {item.ProductName}");
                await _repository.SaveAsync();
                _logger.LogInfo($"Recommendation rejected. RecommendationId: {recommendation.Id}, UserId: {request.UserId}");
                return Unit.Value;
            }

            // Approve: the line now orders the recommended product. The name first requested is kept on the line.
            string replacedProductName = item.ProductName;
            CatalogMaterialMapping? mapping = await _repository.CatalogMaterialMapping.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.CatalogId == recommendation.CatalogId && x.IsActive);

            item.OriginalProductName ??= item.ProductName;
            item.CatalogId = recommendation.CatalogId;
            item.Sku = recommendation.Sku;
            item.ProductName = recommendation.ProductName;
            item.Description = recommendation.Description;
            item.SupplierId = recommendation.SupplierId;
            item.SupplierName = recommendation.SupplierName;
            item.UnitOfMeasure = recommendation.UnitOfMeasure;
            item.Price = recommendation.Price;
            item.Currency = recommendation.Currency;
            item.DiscountPercent = recommendation.DiscountPercent;
            item.MaterialId = mapping?.MaterialId;
            item.MaterialCode = mapping?.MaterialCode;
            item.SupplierStock = recommendation.AvailableStock;
            item.SupplierStockRefreshedOn = recommendation.DateCreated;
            item.StockInHand = await _stockInHandProvider.GetStockInHandAsync(
                buyer.Id, item.StorageLocation, item.MaterialCode, cancellationToken);
            item.AvailabilityStatus = WeeklyBucketRules.GetAvailability(item.SupplierStock, item.ApprovedQuantity);
            item.LineStatus = Common.LINE_RECOMMENDATION_APPROVED;
            recommendation.Status = Common.RECOMMENDATION_APPROVED;
            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_RECOMMENDATION_APPROVED,
                $"{recommendation.RecommendationNumber}: {replacedProductName} -> {recommendation.ProductName}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Recommendation approved. RecommendationId: {recommendation.Id}, ItemId: {item.Id}, UserId: {request.UserId}");
            return Unit.Value;
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
