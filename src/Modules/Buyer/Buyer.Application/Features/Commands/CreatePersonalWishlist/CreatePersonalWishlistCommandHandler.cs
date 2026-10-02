using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreatePersonalWishlist
{
    public class CreatePersonalWishlistCommandHandler : IRequestHandler<CreatePersonalWishlistCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public CreatePersonalWishlistCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<Guid> Handle(CreatePersonalWishlistCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating personal wishlist. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            if (string.IsNullOrWhiteSpace(request.Request.Name))
            {
                _logger.LogError($"Wishlist name is missing. UserId: {request.UserId}");
                throw new BadRequestCustomException("Wishlist name is required.", "Enter a wishlist name.");
            }

            // An empty wishlist is valid: products can be added later.
            List<CatalogItemWriteDto> requested = request.Request.Items ?? new List<CatalogItemWriteDto>();
            if (requested.Any(line => line.CatalogId == Guid.Empty || line.Quantity <= 0))
            {
                _logger.LogError($"A product or a quantity is missing. UserId: {request.UserId}");
                throw new BadRequestCustomException("Quantity must be greater than zero.", "Select every product from the product catalog and enter a quantity.");
            }

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            PersonalWishlist wishlist = new PersonalWishlist
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                OwnerUserId = request.UserId,
                Name = request.Request.Name.Trim(),
                Description = request.Request.Description
            };
            _repository.PersonalWishlist.Create(wishlist);

            if (requested.Count > 0)
            {
                // The same product sent twice is one quantity. Product data is read from the Supplier service, not from the request.
                Dictionary<Guid, decimal> quantities = requested
                    .GroupBy(line => line.CatalogId)
                    .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));
                List<BuyerCatalogItemDto> products = await _supplierApiClient.GetBuyerCatalogStock(quantities.Keys.ToList(), cancellationToken);
                foreach (KeyValuePair<Guid, decimal> line in quantities)
                {
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
            }

            await _repository.SaveAsync();

            _logger.LogInfo($"Personal wishlist created. WishlistId: {wishlist.Id}, UserId: {request.UserId}");
            return wishlist.Id;
        }
    }
}
