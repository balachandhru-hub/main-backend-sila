using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateWeeklyBucketItemQuantity
{
    public class UpdateWeeklyBucketItemQuantityCommandHandler : IRequestHandler<UpdateWeeklyBucketItemQuantityCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateWeeklyBucketItemQuantityCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdateWeeklyBucketItemQuantityCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Changing requested quantity. WeeklyBucketId: {request.WeeklyBucketId}, ItemId: {request.ItemId}, UserId: {request.UserId}");

            if (request.Request.Quantity <= 0)
            {
                _logger.LogError($"Quantity is not greater than zero. ItemId: {request.ItemId}");
                throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a quantity, or remove the product from the bucket.");
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

            if (item.RequestorUserId != request.UserId)
            {
                _logger.LogError($"User is not the requestor of the line. ItemId: {item.Id}, UserId: {request.UserId}");
                throw new ForBiddenCustomException("You did not request this product.", "Only the requestor can change the requested quantity.");
            }

            // The final quantity follows the requested quantity until a reviewer has set another one.
            decimal previousQuantity = item.RequestedQuantity;
            bool reviewerChangedFinalQuantity = item.ApprovedQuantity != item.RequestedQuantity;
            item.RequestedQuantity = request.Request.Quantity;
            if (!reviewerChangedFinalQuantity)
            {
                item.ApprovedQuantity = request.Request.Quantity;
            }

            item.AvailabilityStatus = WeeklyBucketRules.GetAvailability(item.SupplierStock, item.ApprovedQuantity);
            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_QUANTITY_CHANGED,
                $"Product={item.ProductName} RequestedQuantity: {previousQuantity} -> {item.RequestedQuantity}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Requested quantity changed. WeeklyBucketId: {bucket.Id}, ItemId: {item.Id}, UserId: {request.UserId}");
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
