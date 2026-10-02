using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeletePersonalWishlistItem
{
    public class DeletePersonalWishlistItemCommandHandler : IRequestHandler<DeletePersonalWishlistItemCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeletePersonalWishlistItemCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeletePersonalWishlistItemCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Removing product from personal wishlist. WishlistId: {request.WishlistId}, ItemId: {request.ItemId}, UserId: {request.UserId}");

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

            PersonalWishlistItem? item = await _repository.PersonalWishlist.GetItemAsync(wishlist.Id, request.ItemId, cancellationToken);
            if (item == null)
            {
                _logger.LogError($"Personal wishlist product not found. WishlistId: {wishlist.Id}, ItemId: {request.ItemId}");
                throw new NotFoundCustomException("Product not found in the wishlist.", "The product does not belong to this wishlist.");
            }

            _repository.PersonalWishlistItem.Delete(item);
            _repository.PersonalWishlist.Update(wishlist);
            await _repository.SaveAsync();

            _logger.LogInfo($"Product removed from personal wishlist. WishlistId: {wishlist.Id}, ItemId: {item.Id}, UserId: {request.UserId}");
            return Unit.Value;
        }
    }
}
