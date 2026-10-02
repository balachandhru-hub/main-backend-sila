using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    /// <summary>
    /// Writes products, or the stock of products, read from a supplier's own API into that supplier's
    /// catalog. Products are matched by SKU. The integration service calls it after a catalog or
    /// stock pull.
    /// </summary>
    public class SyncSupplierCatalogCommandHandler
        : IRequestHandler<SyncSupplierCatalogCommand, SupplierCatalogSyncResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SyncSupplierCatalogCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierCatalogSyncResultDto> Handle(
            SyncSupplierCatalogCommand request,
            CancellationToken cancellationToken)
        {
            SupplierCatalogSyncRequestDto sync = request.Request;
            _logger.LogInfo($"Syncing supplier catalog. OrganizationId: {sync.OrganizationId}, Mode: {sync.Mode}, Items: {sync.Items.Count}");

            SupplierBusinessProfile? supplier = await _repository.SupplierBusinessProfile
                .FindFirstByConditionAsync(x =>
                    x.OrganizationId == sync.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                _logger.LogError($"Supplier profile not found. OrganizationId: {sync.OrganizationId}");
                throw new NotFoundCustomException(
                    "Supplier profile not found.",
                    "Supplier profile not found.");
            }

            bool stockOnly = string.Equals(sync.Mode, SupplierCatalogSyncRequestDto.MODE_STOCK, StringComparison.OrdinalIgnoreCase);

            // The last entry of a SKU wins when the API repeats one.
            Dictionary<string, SupplierCatalogSyncItemDto> items = sync.Items
                .Where(x => !string.IsNullOrWhiteSpace(x.Sku))
                .GroupBy(x => x.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
            List<string> skus = items.Keys.ToList();

            List<global::Supplier.Domain.Entities.SupplierCatalog> existing = await _repository.SupplierCatalog
                .FindByCondition(x => x.SupplierId == supplier.Id && x.IsActive && x.Sku != null && skus.Contains(x.Sku))
                .AsTracking()
                .ToListAsync(cancellationToken);

            SupplierCatalogSyncResultDto result = new SupplierCatalogSyncResultDto();
            foreach (KeyValuePair<string, SupplierCatalogSyncItemDto> entry in items)
            {
                SupplierCatalogSyncItemDto item = entry.Value;
                global::Supplier.Domain.Entities.SupplierCatalog? catalog = existing.FirstOrDefault(
                    x => string.Equals(x.Sku, entry.Key, StringComparison.OrdinalIgnoreCase));

                if (catalog == null)
                {
                    // Stock of a SKU the catalog does not have says nothing about any product.
                    if (stockOnly || string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.UnitOfMeasure))
                    {
                        result.Skipped++;
                        continue;
                    }

                    _repository.SupplierCatalog.Create(new global::Supplier.Domain.Entities.SupplierCatalog
                    {
                        Id = Guid.NewGuid(),
                        SupplierId = supplier.Id,
                        Sku = entry.Key,
                        CatalogName = item.Name.Trim(),
                        Description = item.Description?.Trim() ?? string.Empty,
                        Price = item.Price,
                        Currency = item.Currency?.Trim().ToUpperInvariant() ?? string.Empty,
                        UnitOfMeasure = item.UnitOfMeasure.Trim(),
                        CatalogType = Common.CATALOG,
                        IsPunchOut = false,
                        AvailableStock = item.AvailableStock,
                        DiscountPercent = item.DiscountPercent
                    });
                    result.Created++;
                    continue;
                }

                if (!stockOnly)
                {
                    catalog.CatalogName = string.IsNullOrWhiteSpace(item.Name) ? catalog.CatalogName : item.Name.Trim();
                    catalog.Description = item.Description?.Trim() ?? catalog.Description;
                    catalog.Price = item.Price ?? catalog.Price;
                    catalog.Currency = string.IsNullOrWhiteSpace(item.Currency) ? catalog.Currency : item.Currency.Trim().ToUpperInvariant();
                    catalog.UnitOfMeasure = string.IsNullOrWhiteSpace(item.UnitOfMeasure) ? catalog.UnitOfMeasure : item.UnitOfMeasure.Trim();
                }

                // A value the API does not send leaves the stored one as it is.
                catalog.AvailableStock = item.AvailableStock ?? catalog.AvailableStock;
                catalog.DiscountPercent = item.DiscountPercent ?? catalog.DiscountPercent;
                _repository.SupplierCatalog.Update(catalog);
                result.Updated++;
            }

            await _repository.SaveAsync();

            _logger.LogInfo($"Supplier catalog synced. SupplierId: {supplier.Id}, Created: {result.Created}, Updated: {result.Updated}, Skipped: {result.Skipped}");
            return result;
        }
    }
}
