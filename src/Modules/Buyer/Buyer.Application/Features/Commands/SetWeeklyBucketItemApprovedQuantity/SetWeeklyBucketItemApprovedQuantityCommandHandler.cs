using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SetWeeklyBucketItemApprovedQuantity
{
    public class SetWeeklyBucketItemApprovedQuantityCommandHandler : IRequestHandler<SetWeeklyBucketItemApprovedQuantityCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SetWeeklyBucketItemApprovedQuantityCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(SetWeeklyBucketItemApprovedQuantityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Setting final quantity. WeeklyBucketId: {request.WeeklyBucketId}, ItemId: {request.ItemId}, UserId: {request.UserId}");

            if (request.Request.Quantity < 0)
            {
                _logger.LogError($"Final quantity is negative. ItemId: {request.ItemId}");
                throw new BadRequestCustomException("Quantity cannot be negative.", "Enter zero to leave the product out of the purchase order.");
            }

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedAsync(request.WeeklyBucketId, buyer.Id, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found. WeeklyBucketId: {request.WeeklyBucketId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Weekly bucket not found.", "No weekly bucket exists for this buyer organization.");
            }

            WeeklyBucketRules.EnsureOpen(bucket, _logger);

            WeeklyBucketItem? item = await _repository.WeeklyBucket.GetItemAsync(bucket.Id, request.ItemId, cancellationToken);
            if (item == null)
            {
                _logger.LogError($"Weekly bucket line not found. WeeklyBucketId: {bucket.Id}, ItemId: {request.ItemId}");
                throw new NotFoundCustomException("Product not found in the weekly bucket.", "The line does not belong to this weekly bucket.");
            }

            // The requested quantity is kept: only the final quantity changes.
            decimal previousQuantity = item.ApprovedQuantity;
            item.ApprovedQuantity = request.Request.Quantity;
            item.AvailabilityStatus = WeeklyBucketRules.GetAvailability(item.SupplierStock, item.ApprovedQuantity);
            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_QUANTITY_CHANGED,
                $"Product={item.ProductName} ApprovedQuantity: {previousQuantity} -> {item.ApprovedQuantity} (requested {item.RequestedQuantity})");
            await _repository.SaveAsync();

            _logger.LogInfo($"Final quantity set. WeeklyBucketId: {bucket.Id}, ItemId: {item.Id}, UserId: {request.UserId}");
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
