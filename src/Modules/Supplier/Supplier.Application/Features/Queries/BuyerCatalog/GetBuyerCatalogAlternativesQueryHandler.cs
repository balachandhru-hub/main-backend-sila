using MediatR;
using Microsoft.EntityFrameworkCore;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using SupplierCatalogEntity = Supplier.Domain.Entities.SupplierCatalog;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogAlternativesQueryHandler
        : IRequestHandler<GetBuyerCatalogAlternativesQuery, List<BuyerCatalogStockDto>>
    {
        private const int MaximumAlternatives = 5;

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBuyerCatalogAlternativesQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<BuyerCatalogStockDto>> Handle(
            GetBuyerCatalogAlternativesQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Buyer Catalog alternatives for CatalogId: {request.CatalogId}, Quantity: {request.Quantity}");

            if (request.Quantity < 0)
            {
                _logger.LogError($"Quantity is negative. CatalogId: {request.CatalogId}, Quantity: {request.Quantity}");
                throw new BadRequestCustomException(
                    "Quantity cannot be negative.",
                    "Send the quantity the alternative product must be able to supply.");
            }

            SupplierCatalogEntity? product = await _repository.SupplierCatalog
                .FindByCondition(x => x.IsActive && x.Id == request.CatalogId)
                .FirstOrDefaultAsync(cancellationToken);

            if (product == null)
            {
                _logger.LogError(
                    $"No catalog found for CatalogId: {request.CatalogId}");

                throw new NotFoundCustomException(
                    "Catalog not found.",
                    $"No active catalog found for CatalogId: {request.CatalogId}");
            }

            // Same commodity first. The class is used only when the commodity gives nothing.
            List<BuyerCatalogStockDto> result = new List<BuyerCatalogStockDto>();
            if (product.Commodity.HasValue)
            {
                long commodity = product.Commodity.Value;
                result = await FindAsync(
                    _repository.SupplierCatalog.FindByCondition(x => x.Commodity == commodity),
                    request,
                    cancellationToken);
            }

            if (result.Count == 0 && product.Class.HasValue)
            {
                _logger.LogInfo(
                    $"No alternative in the same commodity. Falling back to class {product.Class.Value} for CatalogId: {request.CatalogId}");
                long classCode = product.Class.Value;
                result = await FindAsync(
                    _repository.SupplierCatalog.FindByCondition(x => x.Class == classCode),
                    request,
                    cancellationToken);
            }

            _logger.LogInfo(
                $"Returned {result.Count} alternative(s) for CatalogId: {request.CatalogId}");

            return result;
        }

        // Active products other than the requested one that have enough stock, cheapest first.
        private async Task<List<BuyerCatalogStockDto>> FindAsync(
            IQueryable<SupplierCatalogEntity> sameGroup,
            GetBuyerCatalogAlternativesQuery request,
            CancellationToken cancellationToken)
        {
            IQueryable<BuyerCatalogStockDto> alternatives =
                from catalog in sameGroup.Where(
                    x => x.IsActive
                         && x.Id != request.CatalogId
                         && x.AvailableStock != null
                         && x.AvailableStock >= request.Quantity)

                join supplier in _repository.SupplierBusinessProfile.FindByCondition(
                    x => x.IsActive)
                    on catalog.SupplierId equals supplier.Id

                orderby catalog.Price == null, catalog.Price, catalog.CatalogName

                select new BuyerCatalogStockDto
                {
                    CatalogId = catalog.Id,
                    SupplierId = supplier.Id,
                    SupplierName = supplier.OrganizationName,
                    Sku = catalog.Sku,
                    CatalogName = catalog.CatalogName,
                    Description = catalog.Description,
                    Price = catalog.Price,
                    Currency = catalog.Currency,
                    UnitOfMeasure = catalog.UnitOfMeasure,
                    DiscountPercent = catalog.DiscountPercent,
                    AvailableStock = catalog.AvailableStock
                };

            return await alternatives
                .Take(MaximumAlternatives)
                .ToListAsync(cancellationToken);
        }
    }
}
