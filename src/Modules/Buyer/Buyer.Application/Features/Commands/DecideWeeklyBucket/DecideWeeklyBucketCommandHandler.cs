using Buyer.Application.Contracts;
using Buyer.Application.Features.Shared;
using Buyer.Application.Services;
using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DecideWeeklyBucket
{
    public class DecideWeeklyBucketCommandHandler : IRequestHandler<DecideWeeklyBucketCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IOperationsIntegrationClient _integrationClient;
        private readonly IUserContext _userContext;
        private readonly ILoggerManager _logger;

        public DecideWeeklyBucketCommandHandler(
            IRepositoryWrapper repository,
            IOperationsIntegrationClient integrationClient,
            IUserContext userContext,
            ILoggerManager logger)
        {
            _repository = repository;
            _integrationClient = integrationClient;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task<Unit> Handle(DecideWeeklyBucketCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Processing weekly bucket approval. WeeklyBucketId: {request.WeeklyBucketId}, UserId: {request.UserId}");

            if (request.Decision == null || string.IsNullOrWhiteSpace(request.Decision.Status))
            {
                _logger.LogError($"Approval status is missing. WeeklyBucketId: {request.WeeklyBucketId}");
                throw new BadRequestCustomException("Approval status is required.", "Please provide APPROVE or REJECT.");
            }

            if (request.Decision.Status != Common.APPROVED && request.Decision.Status != Common.REJECTED)
            {
                _logger.LogError($"Approval status is invalid. WeeklyBucketId: {request.WeeklyBucketId}, Status: {request.Decision.Status}");
                throw new BadRequestCustomException("Invalid approval status.", "Status must be APPROVE or REJECT.");
            }

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            if (bucket.Status != Common.WEEKLY_BUCKET_PENDING_APPROVAL)
            {
                _logger.LogError($"Weekly bucket is not pending approval. WeeklyBucketId: {bucket.Id}, Status: {bucket.Status}");
                throw new BadRequestCustomException("Weekly bucket is not pending approval.", "Only a frozen weekly bucket can be approved or rejected.");
            }

            WeeklyBucketApprovalFlow? instance = await _repository.WeeklyBucket.GetApprovalFlowAsync(bucket.Id, cancellationToken);
            if (instance == null)
            {
                _logger.LogError($"Approval flow not found for weekly bucket. WeeklyBucketId: {bucket.Id}");
                throw new NotFoundCustomException("Approval flow not found.", "This weekly bucket has no approval instance.");
            }

            List<WeeklyBucketApprovalUserMapping> approvers = await _repository.WeeklyBucket.GetApprovalUsersAsync(instance.Id, cancellationToken);
            WeeklyBucketApprovalUserMapping? current = approvers.FirstOrDefault(x => x.UserId == request.UserId && x.Status == Common.PENDING);
            if (current == null)
            {
                _logger.LogError($"User is not the pending approver. WeeklyBucketId: {bucket.Id}, UserId: {request.UserId}");
                throw new ForBiddenCustomException("You are not the current approver.", "Only the pending approver for this weekly bucket can act.");
            }

            bool previousApproved = approvers.Where(x => x.Order < current.Order).All(x => x.Status == Common.APPROVED);
            if (!previousApproved)
            {
                _logger.LogError($"Earlier approval levels are pending. WeeklyBucketId: {bucket.Id}, Order: {current.Order}");
                throw new BadRequestCustomException("Earlier approval levels are still pending.", "Approvers must act in the configured order.");
            }

            current.Status = request.Decision.Status;
            current.Comment = request.Decision.Comment;
            current.ActedOn = DateTime.UtcNow;

            if (request.Decision.Status == Common.REJECTED)
            {
                // Rejection is final: the bucket stays read-only.
                bucket.Status = Common.WEEKLY_BUCKET_REJECTED;
                bucket.LastError = request.Decision.Comment;
                WeeklyBucketRules.AddAudit(_repository, bucket.Id, request.UserId, Common.AUDIT_REJECTED, request.Decision.Comment);
                await _repository.SaveAsync();
                _logger.LogInfo($"Weekly bucket rejected. WeeklyBucketId: {bucket.Id}, UserId: {request.UserId}");
                return Unit.Value;
            }

            WeeklyBucketRules.AddAudit(_repository, bucket.Id, request.UserId, Common.AUDIT_APPROVED, $"Order={current.Order}");
            if (!approvers.All(x => x.Status == Common.APPROVED))
            {
                await _repository.SaveAsync();
                _logger.LogInfo($"Weekly bucket approval recorded. WeeklyBucketId: {bucket.Id}, Order: {current.Order}");
                return Unit.Value;
            }

            bucket.FinalApprovedOn = DateTime.UtcNow;
            bucket.Status = Common.WEEKLY_BUCKET_APPROVED;
            bucket.LastError = null;
            WeeklyBucketRules.AddAudit(
                _repository, bucket.Id, request.UserId, Common.AUDIT_ERP_STARTED, "Final approval is sending one purchase order per supplier to the configured API.");
            await _repository.SaveAsync();
            _logger.LogInfo($"Weekly bucket final approval stored. WeeklyBucketId: {bucket.Id}, UserId: {request.UserId}");

            WeeklyBucketIntegrationProcessor processor = new WeeklyBucketIntegrationProcessor(
                _repository,
                new BuyerPurchaseDocumentGateway(_integrationClient, _logger),
                _userContext,
                _logger);
            await processor.SendPurchaseOrdersAsync(bucket.Id, request.UserId, cancellationToken);
            if (bucket.Status == Common.WEEKLY_BUCKET_PO_FAILED)
            {
                _logger.LogError($"Approval stored but purchase orders failed. WeeklyBucketId: {bucket.Id}, Error: {bucket.LastError}");
                throw new PreConditionFailedCustomException(
                    "Approval was stored, but not every purchase order was created.",
                    bucket.LastError ?? "The configured purchase order API did not accept the document.");
            }

            _logger.LogInfo($"Purchase orders sent after approval. WeeklyBucketId: {bucket.Id}, Status: {bucket.Status}");
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
