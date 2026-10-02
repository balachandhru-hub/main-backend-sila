using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetWeeklyBucket
{
    public class GetWeeklyBucketQueryHandler : IRequestHandler<GetWeeklyBucketQuery, WeeklyBucketDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIdentityApiClient _identityApiClient;

        public GetWeeklyBucketQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIdentityApiClient identityApiClient)
        {
            _repository = repository;
            _logger = logger;
            _identityApiClient = identityApiClient;
        }

        public async Task<WeeklyBucketDetailDto> Handle(GetWeeklyBucketQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching weekly bucket. WeeklyBucketId: {request.WeeklyBucketId}, OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            BuyerProperty? property = await _repository.WeeklyBucket.GetPropertyAsync(bucket.PropertyId, buyer.Id, cancellationToken);
            List<BuyerOutlet> outlets = await _repository.WeeklyBucket.ListOutletsAsync(buyer.Id, cancellationToken);
            List<WeeklyBucketItem> items = await _repository.WeeklyBucket.GetItemsAsync(bucket.Id, cancellationToken);
            List<WeeklyBucketRecommendation> recommendations = await _repository.WeeklyBucket.GetRecommendationsAsync(bucket.Id, cancellationToken);
            WeeklyBucketApprovalFlow? flow = await _repository.WeeklyBucket.GetApprovalFlowAsync(bucket.Id, cancellationToken);
            List<WeeklyBucketApprovalUserMapping> steps = flow == null
                ? new List<WeeklyBucketApprovalUserMapping>()
                : await _repository.WeeklyBucket.GetApprovalUsersAsync(flow.Id, cancellationToken);
            List<PurchaseDocumentIntegration> integrations = await _repository.WeeklyBucket.GetIntegrationsAsync(bucket.Id, cancellationToken);
            List<WeeklyBucketAudit> audit = await _repository.WeeklyBucket.GetAuditAsync(bucket.Id, cancellationToken);

            // Requestors, the user who froze the bucket, deciders, approvers and audit actors: one call to Identity.
            List<Guid> userIds = items.Select(item => item.RequestorUserId)
                .Concat(steps.Select(step => step.UserId))
                .Concat(recommendations.Where(x => x.DecidedBy != null).Select(x => x.DecidedBy!.Value))
                .Concat(audit.Where(x => x.ActorUserId != null).Select(x => x.ActorUserId!.Value))
                .Concat(bucket.FrozenBy == null ? new List<Guid>() : new List<Guid> { bucket.FrozenBy.Value })
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToList();
            List<IdentityUserDto> users = userIds.Count == 0
                ? new List<IdentityUserDto>()
                : await _identityApiClient.GetUsersByIds(userIds, cancellationToken);

            _logger.LogInfo($"Weekly bucket fetched. WeeklyBucketId: {bucket.Id}, Lines: {items.Count}");
            return new WeeklyBucketDetailDto
            {
                Id = bucket.Id,
                BucketCode = bucket.BucketCode,
                WeekNumber = bucket.WeekNumber,
                Year = bucket.Year,
                PropertyId = bucket.PropertyId,
                PropertyName = property?.PropertyName,
                PlantCode = bucket.PlantCode,
                CompanyCode = bucket.CompanyCode,
                Status = bucket.Status,
                IsFrozen = bucket.Status != Common.WEEKLY_BUCKET_OPEN,
                FrozenBy = bucket.FrozenBy,
                FrozenByName = users.FirstOrDefault(x => x.UserId == bucket.FrozenBy)?.Name,
                FrozenOn = bucket.FrozenOn,
                FinalApprovedOn = bucket.FinalApprovedOn,
                ApprovalName = bucket.ApprovalName,
                LastError = bucket.LastError,
                Items = items.Select(item => new WeeklyBucketItemDto
                {
                    Id = item.Id,
                    CatalogId = item.CatalogId,
                    Sku = item.Sku,
                    ProductName = item.ProductName,
                    Description = item.Description,
                    OriginalProductName = item.OriginalProductName,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    SupplierId = item.SupplierId,
                    SupplierName = item.SupplierName,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Price = item.Price,
                    Currency = item.Currency,
                    DiscountPercent = item.DiscountPercent,
                    RequestedQuantity = item.RequestedQuantity,
                    ApprovedQuantity = item.ApprovedQuantity,
                    SupplierStock = item.SupplierStock,
                    SupplierStockRefreshedOn = item.SupplierStockRefreshedOn,
                    StockInHand = item.StockInHand,
                    AvailabilityStatus = item.AvailabilityStatus,
                    LineStatus = item.LineStatus,
                    RequestorUserId = item.RequestorUserId,
                    RequestorName = users.FirstOrDefault(x => x.UserId == item.RequestorUserId)?.Name,
                    OutletId = item.OutletId,
                    OutletName = outlets.FirstOrDefault(x => x.Id == item.OutletId)?.OutletName,
                    StorageLocation = item.StorageLocation,
                    DateCreated = item.DateCreated
                }).ToList(),
                Recommendations = recommendations.Select(recommendation => new WeeklyBucketRecommendationDto
                {
                    Id = recommendation.Id,
                    RecommendationNumber = recommendation.RecommendationNumber,
                    WeeklyBucketItemId = recommendation.WeeklyBucketItemId,
                    CatalogId = recommendation.CatalogId,
                    Sku = recommendation.Sku,
                    ProductName = recommendation.ProductName,
                    SupplierId = recommendation.SupplierId,
                    SupplierName = recommendation.SupplierName,
                    Price = recommendation.Price,
                    Currency = recommendation.Currency,
                    AvailableStock = recommendation.AvailableStock,
                    Quantity = recommendation.Quantity,
                    Status = recommendation.Status,
                    DecidedBy = recommendation.DecidedBy,
                    DecidedByName = users.FirstOrDefault(x => x.UserId == recommendation.DecidedBy)?.Name,
                    DecidedOn = recommendation.DecidedOn
                }).ToList(),
                ApprovalSteps = steps.Select(step => new WeeklyBucketApprovalStepDto
                {
                    UserId = step.UserId,
                    Name = users.FirstOrDefault(x => x.UserId == step.UserId)?.Name,
                    Email = users.FirstOrDefault(x => x.UserId == step.UserId)?.Email,
                    Order = step.Order,
                    Status = step.Status,
                    Comment = step.Comment,
                    ActedOn = step.ActedOn
                }).ToList(),
                PurchaseOrders = integrations.Select(integration => new WeeklyBucketPurchaseOrderDto
                {
                    SupplierId = integration.SupplierOrganizationId,
                    SupplierName = items.FirstOrDefault(x => x.SupplierId == integration.SupplierOrganizationId)?.SupplierName,
                    Status = integration.Status,
                    DocumentNumber = integration.ExternalDocumentNumber,
                    ErrorMessage = integration.ErrorMessage
                }).ToList(),
                Audit = audit.Select(row => new WeeklyBucketAuditDto
                {
                    Action = row.Action,
                    Detail = row.Detail,
                    ActorUserId = row.ActorUserId,
                    ActorName = users.FirstOrDefault(x => x.UserId == row.ActorUserId)?.Name,
                    DateCreated = row.DateCreated
                }).ToList()
            };
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
