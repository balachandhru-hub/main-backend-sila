using MediatR;
using Microsoft.EntityFrameworkCore;
using Supplier.Application.Contracts;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.BuyerCatalog
{
    public class GetBuyerCatalogStockQueryHandler
        : IRequestHandler<GetBuyerCatalogStockQuery, List<BuyerCatalogStockDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IOperationsApiClient _operationsApiClient;

        public GetBuyerCatalogStockQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IOperationsApiClient operationsApiClient)
        {
            _repository = repository;
            _logger = logger;
            _operationsApiClient = operationsApiClient;
        }

        public async Task<List<BuyerCatalogStockDto>> Handle(
            GetBuyerCatalogStockQuery request,
            CancellationToken cancellationToken)
        {
            List<Guid> catalogIds = (request.CatalogIds ?? new List<Guid>())
                .Distinct()
                .ToList();

            _logger.LogInfo(
                $"Fetching Buyer Catalog stock. CatalogIds: {catalogIds.Count}");

            if (catalogIds.Count == 0)
            {
                _logger.LogInfo("No catalog ids were sent. Returning an empty list.");
                return new List<BuyerCatalogStockDto>();
            }

            // A supplier that publishes its stock through its own API is asked for it now, so the
            // figures below are current. Suppliers without a stock API keep the stock they typed in.
            List<Guid> supplierOrganizationIds = await (
                from catalog in _repository.SupplierCatalog.FindByCondition(
                    x => x.IsActive && catalogIds.Contains(x.Id))
                join supplier in _repository.SupplierBusinessProfile.FindByCondition(
                    x => x.IsActive)
                    on catalog.SupplierId equals supplier.Id
                select supplier.OrganizationId
            ).Distinct().ToListAsync(cancellationToken);
            await _operationsApiClient.RefreshProductStock(supplierOrganizationIds, cancellationToken);

            IQueryable<BuyerCatalogStockDto> catalogQuery =
                from catalog in _repository.SupplierCatalog.FindByCondition(
                    x => x.IsActive && catalogIds.Contains(x.Id))

                join supplier in _repository.SupplierBusinessProfile.FindByCondition(
                    x => x.IsActive)
                    on catalog.SupplierId equals supplier.Id

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

            List<BuyerCatalogStockDto> result = await catalogQuery.ToListAsync(cancellationToken);

            _logger.LogInfo(
                $"Returned {result.Count} catalog stock record(s) for {catalogIds.Count} catalog id(s).");

            return result;
        }
    }
}
