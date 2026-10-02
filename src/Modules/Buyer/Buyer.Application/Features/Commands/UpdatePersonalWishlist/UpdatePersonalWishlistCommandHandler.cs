using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdatePersonalWishlist
{
    public class UpdatePersonalWishlistCommandHandler : IRequestHandler<UpdatePersonalWishlistCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdatePersonalWishlistCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdatePersonalWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating personal wishlist. WishlistId: {request.WishlistId}, UserId: {request.UserId}");

            if (string.IsNullOrWhiteSpace(request.Request.Name))
            {
                _logger.LogError($"Wishlist name is missing. WishlistId: {request.WishlistId}");
                throw new BadRequestCustomException("Wishlist name is required.", "Enter a wishlist name.");
            }

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

            wishlist.Name = request.Request.Name.Trim();
            wishlist.Description = request.Request.Description;
            _repository.PersonalWishlist.Update(wishlist);
            await _repository.SaveAsync();

            _logger.LogInfo($"Personal wishlist updated. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
            return Unit.Value;
        }
    }
}
