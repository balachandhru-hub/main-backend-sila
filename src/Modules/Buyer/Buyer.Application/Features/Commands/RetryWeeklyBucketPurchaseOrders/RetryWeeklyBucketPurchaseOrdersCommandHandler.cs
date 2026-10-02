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

namespace Buyer.Application.Features.Commands.RetryWeeklyBucketPurchaseOrders
{
    public class RetryWeeklyBucketPurchaseOrdersCommandHandler : IRequestHandler<RetryWeeklyBucketPurchaseOrdersCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IOperationsIntegrationClient _integrationClient;
        private readonly IUserContext _userContext;
        private readonly ILoggerManager _logger;

        public RetryWeeklyBucketPurchaseOrdersCommandHandler(
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

        public async Task<Unit> Handle(RetryWeeklyBucketPurchaseOrdersCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Retrying weekly bucket purchase orders. WeeklyBucketId: {request.WeeklyBucketId}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            if (bucket.Status != Common.WEEKLY_BUCKET_PO_FAILED)
            {
                _logger.LogError($"Weekly bucket has no failed purchase order. WeeklyBucketId: {bucket.Id}, Status: {bucket.Status}");
                throw new BadRequestCustomException(
                    "Purchase orders cannot be retried.",
                    "Only a weekly bucket whose purchase orders failed can be retried.");
            }

            WeeklyBucketRules.AddAudit(_repository, bucket.Id, request.UserId, Common.AUDIT_RETRY, bucket.LastError);
            await _repository.SaveAsync();

            // Only the suppliers without a purchase order are sent again.
            WeeklyBucketIntegrationProcessor processor = new WeeklyBucketIntegrationProcessor(
                _repository,
                new BuyerPurchaseDocumentGateway(_integrationClient, _logger),
                _userContext,
                _logger);
            await processor.SendPurchaseOrdersAsync(bucket.Id, request.UserId, cancellationToken);
            if (bucket.Status == Common.WEEKLY_BUCKET_PO_FAILED)
            {
                _logger.LogError($"Purchase order retry failed. WeeklyBucketId: {bucket.Id}, Error: {bucket.LastError}");
                throw new PreConditionFailedCustomException(
                    "Not every purchase order was created.",
                    bucket.LastError ?? "The configured purchase order API did not accept the document.");
            }

            _logger.LogInfo($"Purchase orders created on retry. WeeklyBucketId: {bucket.Id}, Status: {bucket.Status}");
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
