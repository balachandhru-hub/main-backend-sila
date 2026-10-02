using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetPersonalWishlists
{
    public class GetPersonalWishlistsQueryHandler : IRequestHandler<GetPersonalWishlistsQuery, List<PersonalWishlistListItemDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetPersonalWishlistsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<PersonalWishlistListItemDto>> Handle(GetPersonalWishlistsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching personal wishlists. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            // Only the caller's own wishlists.
            List<PersonalWishlist> wishlists = await _repository.PersonalWishlist
                .FindByCondition(x => x.BuyerId == buyer.Id && x.OwnerUserId == request.UserId && x.IsActive)
                .OrderByDescending(x => x.DateUpdated)
                .ToListAsync(cancellationToken);
            List<Guid> wishlistIds = wishlists.Select(wishlist => wishlist.Id).ToList();
            List<Guid> itemWishlistIds = await _repository.PersonalWishlistItem
                .FindByCondition(x => wishlistIds.Contains(x.PersonalWishlistId) && x.IsActive)
                .Select(x => x.PersonalWishlistId)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Personal wishlists fetched. Count: {wishlists.Count}, UserId: {request.UserId}");
            return wishlists.Select(wishlist => new PersonalWishlistListItemDto
            {
                Id = wishlist.Id,
                Name = wishlist.Name,
                Description = wishlist.Description,
                ItemCount = itemWishlistIds.Count(id => id == wishlist.Id),
                DateCreated = wishlist.DateCreated,
                DateUpdated = wishlist.DateUpdated
            }).ToList();
        }
    }
}
