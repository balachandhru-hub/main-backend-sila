using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.RemoveWeeklyBucketItem
{
    public class RemoveWeeklyBucketItemCommandHandler : IRequestHandler<RemoveWeeklyBucketItemCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public RemoveWeeklyBucketItemCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(RemoveWeeklyBucketItemCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Removing weekly bucket line. WeeklyBucketId: {request.WeeklyBucketId}, ItemId: {request.ItemId}, UserId: {request.UserId}");

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

            // The requestor removes his own line. A reviewer (store manager or buyer administrator) removes any line.
            if (item.RequestorUserId != request.UserId
                && request.RoleId != Common.BUYER_ADMIN_ROLE_ID
                && request.RoleId != Common.STORE_MANAGER_ROLE_ID)
            {
                _logger.LogError($"User may not remove the line. ItemId: {item.Id}, UserId: {request.UserId}");
                throw new ForBiddenCustomException("You did not request this product.", "Only the requestor or a reviewer can remove a product.");
            }

            List<WeeklyBucketRecommendation> recommendations = (await _repository.WeeklyBucket.GetRecommendationsAsync(bucket.Id, cancellationToken))
                .Where(x => x.WeeklyBucketItemId == item.Id)
                .ToList();
            _repository.WeeklyBucketRecommendation.DeleteRange(recommendations);
            _repository.WeeklyBucketItem.Delete(item);
            WeeklyBucketRules.AddAudit(
                _repository,
                bucket.Id,
                request.UserId,
                Common.AUDIT_ITEM_REMOVED,
                $"Product={item.ProductName} RequestedQuantity={item.RequestedQuantity} RequestorUserId={item.RequestorUserId}");
            await _repository.SaveAsync();

            _logger.LogInfo($"Weekly bucket line removed. WeeklyBucketId: {bucket.Id}, ItemId: {item.Id}, UserId: {request.UserId}");
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
