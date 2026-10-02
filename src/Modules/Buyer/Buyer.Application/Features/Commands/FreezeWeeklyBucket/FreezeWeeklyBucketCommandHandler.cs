using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.FreezeWeeklyBucket
{
    public class FreezeWeeklyBucketCommandHandler : IRequestHandler<FreezeWeeklyBucketCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public FreezeWeeklyBucketCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(FreezeWeeklyBucketCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Freezing weekly bucket. WeeklyBucketId: {request.WeeklyBucketId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            WeeklyBucketRules.EnsureOpen(bucket, _logger);

            List<WeeklyBucketItem> items = await _repository.WeeklyBucket.GetItemsAsync(bucket.Id, cancellationToken);

            // An unavailable line is left out unless a recommendation was approved for it. It does not block.
            // A partially available line proceeds with its final quantity: the reviewer lowers it before freezing.
            List<WeeklyBucketItem> excluded = items
                .Where(item => item.AvailabilityStatus == Common.AVAILABILITY_UNAVAILABLE
                               && item.LineStatus != Common.LINE_RECOMMENDATION_APPROVED)
                .ToList();
            List<WeeklyBucketItem> proceeding = items
                .Where(item => !excluded.Contains(item) && item.ApprovedQuantity > 0)
                .ToList();
            if (proceeding.Count == 0)
            {
                _logger.LogError($"Weekly bucket has no line to order. WeeklyBucketId: {bucket.Id}, Lines: {items.Count}, Excluded: {excluded.Count}");
                throw new BadRequestCustomException(
                    "Weekly bucket has no product to order.",
                    "At least one available product with a quantity greater than zero is needed to freeze the bucket.");
            }

            // A mapping saved after the line was added is picked up here.
            List<Guid> unmappedCatalogIds = proceeding
                .Where(item => string.IsNullOrWhiteSpace(item.MaterialCode))
                .Select(item => item.CatalogId)
                .Distinct()
                .ToList();
            if (unmappedCatalogIds.Count > 0)
            {
                List<CatalogMaterialMapping> mappings = await _repository.CatalogMaterialMapping
                    .FindByCondition(x => x.BuyerId == buyer.Id && unmappedCatalogIds.Contains(x.CatalogId) && x.IsActive)
                    .ToListAsync(cancellationToken);
                foreach (WeeklyBucketItem item in proceeding.Where(item => string.IsNullOrWhiteSpace(item.MaterialCode)))
                {
                    CatalogMaterialMapping? mapping = mappings.FirstOrDefault(x => x.CatalogId == item.CatalogId);
                    item.MaterialId = mapping?.MaterialId;
                    item.MaterialCode = mapping?.MaterialCode;
                }
            }

            List<string> unmappedProducts = proceeding
                .Where(item => string.IsNullOrWhiteSpace(item.MaterialCode))
                .Select(item => item.ProductName)
                .Distinct()
                .ToList();
            if (unmappedProducts.Count > 0)
            {
                _logger.LogError($"Products without a material mapping. WeeklyBucketId: {bucket.Id}, Count: {unmappedProducts.Count}");
                throw new BadRequestCustomException(
                    "Material mapping is missing.",
                    $"Map these products to an Item Master material before freezing: {string.Join(", ", unmappedProducts)}.");
            }

            // The approval flow comes from the property of the bucket.
            BuyerProperty property = await WeeklyBucketRules.GetPropertyAsync(
                _repository, _logger, bucket.PropertyId, buyer.Id, cancellationToken);
            if (property.MasterApprovalFlowId == null || property.MasterApprovalFlowId == Guid.Empty)
            {
                _logger.LogError($"Property has no approval flow. PropertyId: {property.Id}, WeeklyBucketId: {bucket.Id}");
                throw new BadRequestCustomException(
                    "Approval flow is required.",
                    $"The property {property.PropertyName} has no approval flow. Assign a WEEKLY_BUCKET approval flow to the property.");
            }

            MasterApprovalFlow? flow = await _repository.MasterApprovalFlow.FindFirstByConditionAsync(
                x => x.Id == property.MasterApprovalFlowId && x.BuyerId == buyer.Id && x.IsActive);
            if (flow == null)
            {
                _logger.LogError($"Approval flow not found. ApprovalFlowId: {property.MasterApprovalFlowId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Approval flow not found.", "The approval flow of the property does not belong to this buyer organization.");
            }

            if (!string.Equals(flow.Type, Common.WEEKLY_BUCKET_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError($"Approval flow type is not WEEKLY_BUCKET. ApprovalFlowId: {flow.Id}, Type: {flow.Type}");
                throw new BadRequestCustomException("Approval flow type is not Weekly Bucket.", "Assign an approval configuration of type WEEKLY_BUCKET to the property.");
            }

            await CopyApprovalUsersAsync(bucket, flow, cancellationToken);

            foreach (WeeklyBucketItem item in excluded)
            {
                item.LineStatus = Common.LINE_EXCLUDED;
            }

            bucket.Status = Common.WEEKLY_BUCKET_PENDING_APPROVAL;
            bucket.FrozenBy = request.UserId;
            bucket.FrozenOn = DateTime.UtcNow;
            bucket.MasterApprovalFlowId = flow.Id;
            bucket.ApprovalName = flow.ApprovalName;
            bucket.LastError = null;
            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_FROZEN,
                $"Lines={items.Count} Proceeding={proceeding.Count} Excluded={excluded.Count}");
            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_APPROVAL_STARTED,
                $"ApprovalFlow={flow.ApprovalName}");
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Weekly bucket frozen. WeeklyBucketId: {bucket.Id}, Proceeding: {proceeding.Count}, Excluded: {excluded.Count}, UserId: {request.UserId}");
            return Unit.Value;
        }

        private async Task CopyApprovalUsersAsync(WeeklyBucket bucket, MasterApprovalFlow flow, CancellationToken cancellationToken)
        {
            List<ApprovalFlowUserMapping> templateUsers = await _repository.ApprovalFlowUserMapping
                .FindByCondition(x => x.ApprovalFlowId == flow.Id && x.IsActive)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);
            if (templateUsers.Count == 0)
            {
                _logger.LogError($"Approval flow has no approvers. ApprovalFlowId: {flow.Id}");
                throw new BadRequestCustomException("Approval flow has no approvers.", "Add approvers to the approval flow of the property.");
            }

            WeeklyBucketApprovalFlow instance = new WeeklyBucketApprovalFlow
            {
                Id = Guid.NewGuid(),
                WeeklyBucketId = bucket.Id,
                ApprovalCode = flow.ApprovalCode,
                ApprovalName = flow.ApprovalName,
                MasterApprovalFlowId = flow.Id,
                Type = flow.Type,
                TotalAmount = flow.TotalAmount,
                Currency = flow.Currency
            };
            _repository.WeeklyBucketApprovalFlow.Create(instance);

            foreach (ApprovalFlowUserMapping templateUser in templateUsers)
            {
                _repository.WeeklyBucketApprovalUserMapping.Create(new WeeklyBucketApprovalUserMapping
                {
                    Id = Guid.NewGuid(),
                    WeeklyBucketApprovalFlowId = instance.Id,
                    UserId = templateUser.UserId,
                    Order = templateUser.Order,
                    Status = Common.PENDING
                });
            }
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
