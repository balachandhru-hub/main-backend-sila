using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetPersonalWishlist
{
    public class GetPersonalWishlistQueryHandler : IRequestHandler<GetPersonalWishlistQuery, PersonalWishlistResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetPersonalWishlistQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<PersonalWishlistResponseDto> Handle(GetPersonalWishlistQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching personal wishlist. WishlistId: {request.WishlistId}, UserId: {request.UserId}");

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

            _logger.LogInfo($"Personal wishlist fetched. WishlistId: {wishlist.Id}, Products: {items.Count}");
            return new PersonalWishlistResponseDto
            {
                Id = wishlist.Id,
                Name = wishlist.Name,
                Description = wishlist.Description,
                DateCreated = wishlist.DateCreated,
                DateUpdated = wishlist.DateUpdated,
                Items = items.Select(item => new PersonalWishlistItemDto
                {
                    Id = item.Id,
                    CatalogId = item.CatalogId,
                    Sku = item.Sku,
                    ProductName = item.ProductName,
                    Description = item.Description,
                    SupplierId = item.SupplierId,
                    SupplierName = item.SupplierName,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Price = item.Price,
                    Currency = item.Currency,
                    DiscountPercent = item.DiscountPercent,
                    Quantity = item.Quantity
                }).ToList()
            };
        }
    }
}
