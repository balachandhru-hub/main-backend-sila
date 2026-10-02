using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    /// <summary>
    /// Updates the SKU, available stock and discount of one catalog product of the signed-in supplier.
    /// Nothing else on the product is touched.
    /// </summary>
    public class UpdateSupplierCatalogStockCommandHandler
        : IRequestHandler<UpdateSupplierCatalogStockCommand, bool>
    {
        private const int SKU_MAX_LENGTH = 100;

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateSupplierCatalogStockCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            UpdateSupplierCatalogStockCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating supplier catalog stock. CatalogId: {request.Id}, OrganizationId: {request.OrganizationId}");

            if (request.Stock == null)
            {
                _logger.LogError("Stock details are missing.");
                throw new BadRequestCustomException(
                    "Invalid request.",
                    "Stock details are required.");
            }

            string? sku = string.IsNullOrWhiteSpace(request.Stock.Sku) ? null : request.Stock.Sku.Trim();
            if (sku != null && sku.Length > SKU_MAX_LENGTH)
            {
                _logger.LogError($"SKU is too long. CatalogId: {request.Id}");
                throw new BadRequestCustomException(
                    "Invalid SKU.",
                    $"The SKU can have up to {SKU_MAX_LENGTH} characters.");
            }

            if (request.Stock.AvailableStock.HasValue && request.Stock.AvailableStock.Value < 0)
            {
                _logger.LogError("Available stock cannot be negative.");
                throw new BadRequestCustomException(
                    "Available stock cannot be negative.",
                    "Enter zero or a positive available stock.");
            }

            if (request.Stock.DiscountPercent.HasValue &&
                (request.Stock.DiscountPercent.Value < 0 || request.Stock.DiscountPercent.Value > 100))
            {
                _logger.LogError("Discount percent must be between 0 and 100.");
                throw new BadRequestCustomException(
                    "Invalid discount percent.",
                    "Discount percent must be between 0 and 100.");
            }

            SupplierBusinessProfile? supplier = await _repository.SupplierBusinessProfile
                .FindFirstByConditionAsync(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                _logger.LogError($"Supplier profile not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException(
                    "Supplier profile not found.",
                    "Supplier profile not found.");
            }

            // Only the supplier's own products can be changed.
            global::Supplier.Domain.Entities.SupplierCatalog? catalog = await _repository.SupplierCatalog
                .FindFirstByConditionAsync(x =>
                    x.Id == request.Id &&
                    x.SupplierId == supplier.Id &&
                    x.IsActive);

            if (catalog == null)
            {
                _logger.LogError($"Supplier catalog not found. CatalogId: {request.Id}, SupplierId: {supplier.Id}");
                throw new NotFoundCustomException(
                    "Supplier catalog not found.",
                    "This product is not in your catalog.");
            }

            catalog.Sku = sku;
            catalog.AvailableStock = request.Stock.AvailableStock;
            catalog.DiscountPercent = request.Stock.DiscountPercent;
            _repository.SupplierCatalog.Update(catalog);

            await _repository.SaveAsync();

            _logger.LogInfo($"Supplier catalog stock updated. CatalogId: {catalog.Id}, SupplierId: {supplier.Id}");
            return true;
        }
    }
}
