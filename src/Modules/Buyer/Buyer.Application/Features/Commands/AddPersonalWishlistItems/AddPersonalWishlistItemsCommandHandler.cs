using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.AddPersonalWishlistItems
{
    public class AddPersonalWishlistItemsCommandHandler : IRequestHandler<AddPersonalWishlistItemsCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public AddPersonalWishlistItemsCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<Unit> Handle(AddPersonalWishlistItemsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Adding products to personal wishlist. WishlistId: {request.WishlistId}, UserId: {request.UserId}");

            List<CatalogItemWriteDto> requested = request.Request.Items ?? new List<CatalogItemWriteDto>();
            if (requested.Count == 0)
            {
                _logger.LogError($"No products were sent. WishlistId: {request.WishlistId}");
                throw new BadRequestCustomException("No products to add.", "Select at least one product from the product catalog.");
            }

            if (requested.Any(line => line.CatalogId == Guid.Empty || line.Quantity <= 0))
            {
                _logger.LogError($"A product or a quantity is missing. WishlistId: {request.WishlistId}");
                throw new BadRequestCustomException("Quantity must be greater than zero.", "Select every product from the product catalog and enter a quantity.");
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

            Dictionary<Guid, decimal> quantities = requested
                .GroupBy(line => line.CatalogId)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));
            List<PersonalWishlistItem> existingItems = await _repository.PersonalWishlist.GetItemsAsync(wishlist.Id, cancellationToken);

            // Product data is read from the Supplier service only for products that are not in the list yet.
            List<Guid> newCatalogIds = quantities.Keys
                .Where(catalogId => !existingItems.Any(x => x.CatalogId == catalogId))
                .ToList();
            List<BuyerCatalogItemDto> products = newCatalogIds.Count == 0
                ? new List<BuyerCatalogItemDto>()
                : await _supplierApiClient.GetBuyerCatalogStock(newCatalogIds, cancellationToken);

            foreach (KeyValuePair<Guid, decimal> line in quantities)
            {
                // A product already in the list has its quantity increased.
                PersonalWishlistItem? existing = existingItems.FirstOrDefault(x => x.CatalogId == line.Key);
                if (existing != null)
                {
                    existing.Quantity += line.Value;
                    continue;
                }

                BuyerCatalogItemDto? product = products.FirstOrDefault(x => x.CatalogId == line.Key);
                if (product == null)
                {
                    _logger.LogError($"Product not found in the product catalog. CatalogId: {line.Key}");
                    throw new NotFoundCustomException("Product not found.", "Select a product from the product catalog.");
                }

                _repository.PersonalWishlistItem.Create(new PersonalWishlistItem
                {
                    Id = Guid.NewGuid(),
                    PersonalWishlistId = wishlist.Id,
                    CatalogId = line.Key,
                    Sku = product.Sku,
                    ProductName = product.CatalogName ?? product.Description ?? string.Empty,
                    Description = product.Description,
                    SupplierId = product.SupplierId,
                    SupplierName = product.SupplierName,
                    UnitOfMeasure = product.UnitOfMeasure,
                    Price = product.Price,
                    Currency = product.Currency,
                    DiscountPercent = product.DiscountPercent,
                    Quantity = line.Value
                });
            }

            // Marks the wishlist as changed so its updated date follows its products.
            _repository.PersonalWishlist.Update(wishlist);
            await _repository.SaveAsync();

            _logger.LogInfo($"Products added to personal wishlist. WishlistId: {wishlist.Id}, Products: {quantities.Count}, UserId: {request.UserId}");
            return Unit.Value;
        }
    }
}
