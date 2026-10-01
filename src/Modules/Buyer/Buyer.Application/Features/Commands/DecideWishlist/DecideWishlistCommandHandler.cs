using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DecideWishlist
{
    public class DecideWishlistCommandHandler : IRequestHandler<DecideWishlistCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DecideWishlistCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(DecideWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Processing wishlist approval. WishlistId: {request.WishlistId}, UserId: {request.UserId}");

            if (request.Decision == null || string.IsNullOrWhiteSpace(request.Decision.Status))
            {
                throw new BadRequestCustomException("Approval status is required.", "Please provide APPROVE or REJECT.");
            }

            if (request.Decision.Status != Common.APPROVED && request.Decision.Status != Common.REJECTED)
            {
                throw new BadRequestCustomException("Invalid approval status.", "Status must be APPROVE or REJECT.");
            }

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            Wishlist? wishlist = await _repository.Wishlist.GetTrackedAsync(request.WishlistId, buyer.Id, cancellationToken);
            if (wishlist == null)
            {
                _logger.LogError($"Wishlist not found. WishlistId: {request.WishlistId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Wishlist not found.", "No wishlist exists for this buyer organization.");
            }

            if (wishlist.Status != Common.WISHLIST_PENDING_APPROVAL)
            {
                throw new BadRequestCustomException("Wishlist is not pending approval.", "Only a submitted wishlist can be approved or rejected.");
            }

            WishlistApprovalFlow? instance = await _repository.Wishlist.GetApprovalFlowAsync(wishlist.Id, cancellationToken);
            if (instance == null)
            {
                _logger.LogError($"Approval flow not found for wishlist. WishlistId: {wishlist.Id}");
                throw new NotFoundCustomException("Approval flow not found.", "This wishlist has no approval instance.");
            }

            List<WishlistApprovalUserMapping> approvers = await _repository.Wishlist.GetApprovalUsersAsync(instance.Id, cancellationToken);
            WishlistApprovalUserMapping? current = approvers.FirstOrDefault(x => x.UserId == request.UserId && x.Status == Common.PENDING);
            if (current == null)
            {
                _logger.LogError($"User is not the pending approver. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
                throw new ForBiddenCustomException("You are not the current approver.", "Only the pending approver for this wishlist can act.");
            }

            bool previousApproved = approvers.Where(x => x.Order < current.Order).All(x => x.Status == Common.APPROVED);
            if (!previousApproved)
            {
                throw new BadRequestCustomException("Earlier approval levels are still pending.", "Approvers must act in the configured order.");
            }

            current.Status = request.Decision.Status;
            current.Comment = request.Decision.Comment;
            current.ActedOn = DateTime.UtcNow;

            if (request.Decision.Status == Common.REJECTED)
            {
                wishlist.Status = Common.WISHLIST_REJECTED;
                wishlist.LastError = request.Decision.Comment;
                AddAudit(wishlist.Id, request.UserId, Common.AUDIT_REJECTED, request.Decision.Comment);
                await _repository.SaveAsync();
                _logger.LogInfo($"Wishlist rejected. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
                return Unit.Value;
            }

            AddAudit(wishlist.Id, request.UserId, Common.AUDIT_APPROVED, $"Order={current.Order}");
            if (!approvers.All(x => x.Status == Common.APPROVED))
            {
                await _repository.SaveAsync();
                _logger.LogInfo($"Wishlist approval recorded. WishlistId: {wishlist.Id}, Order: {current.Order}");
                return Unit.Value;
            }

            wishlist.FinalApprovedOn = DateTime.UtcNow;
            wishlist.Status = Common.WISHLIST_ERP_PROCESSING;
            wishlist.LastError = null;
            AddAudit(wishlist.Id, request.UserId, Common.AUDIT_ERP_STARTED, "Final approval started buyer ERP processing.");
            await _repository.SaveAsync();
            _logger.LogInfo($"Wishlist final approval stored. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
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

        private void AddAudit(Guid wishlistId, Guid userId, string action, string? detail)
        {
            _repository.Wishlist.Add(new WishlistAudit
            {
                Id = Guid.NewGuid(),
                WishlistId = wishlistId,
                Action = action,
                Detail = detail,
                ActorUserId = userId
            });
        }
    }
}
