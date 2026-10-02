using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeletePersonalWishlist
{
    public class DeletePersonalWishlistCommandHandler : IRequestHandler<DeletePersonalWishlistCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeletePersonalWishlistCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeletePersonalWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting personal wishlist. WishlistId: {request.WishlistId}, UserId: {request.UserId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            // Only the owner finds the wishlist.
            PersonalWishlist? wishlist = await _repository.PersonalWishlist.GetTrackedAsync(
                request.WishlistId, buyer.Id, request.UserId, cancellationToken);
            if (wishlist == null)
            {
                _logger.LogError($"Personal wishlist not found. WishlistId: {request.WishlistId}, UserId: {request.UserId}");
                throw new NotFoundCustomException("Wishlist not found.", "You have no wishlist with this id.");
            }

            List<PersonalWishlistItem> items = await _repository.PersonalWishlist.GetItemsAsync(wishlist.Id, cancellationToken);
            _repository.PersonalWishlistItem.DeleteRange(items);
            _repository.PersonalWishlist.Delete(wishlist);
            await _repository.SaveAsync();

            _logger.LogInfo($"Personal wishlist deleted. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
            return Unit.Value;
        }
    }
}
