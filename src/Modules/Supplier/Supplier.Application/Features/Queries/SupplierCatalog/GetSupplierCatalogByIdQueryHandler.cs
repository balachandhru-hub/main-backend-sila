using MediatR;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;

namespace Supplier.Application.Features.Queries.SupplierCatalog
{
    public class GetSupplierCatalogByIdQueryHandler
        : IRequestHandler<GetSupplierCatalogByIdQuery, List<SupplierCatalogByIdDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierCatalogByIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SupplierCatalogByIdDto>> Handle(
            GetSupplierCatalogByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Supplier Catalog for CatalogId: {request.CatalogId}");

            if (!request.CatalogId.HasValue)
            {
                throw new NotFoundCustomException(
                    "Catalog ID is required.",
                    "Catalog ID was not provided.");
            }

            var catalogQuery =
                from catalog in _repository.SupplierCatalog.FindByCondition(
                    x => x.IsActive && x.Id == request.CatalogId.Value)

                join supplier in _repository.SupplierBusinessProfile.FindByCondition(
                    x => x.IsActive)
                    on catalog.SupplierId equals supplier.Id

                select new SupplierCatalogByIdDto
                {
                    SupplierId = supplier.Id,
                    CatalogId = catalog.Id,

                    SupplierName = supplier.OrganizationName,
                    CatalogName = catalog.CatalogName,
                    Description = catalog.Description,
                    Price = catalog.Price,
                    Currency = catalog.Currency,
                    UnitOfMeasure = catalog.UnitOfMeasure,
                    Sku = catalog.Sku,
                    AvailableStock = catalog.AvailableStock,
                    DiscountPercent = catalog.DiscountPercent,

                    Segment = catalog.Segment,
                    Family = catalog.Family,
                    Class = catalog.Class,
                    Commodity = catalog.Commodity
                };

            var result = catalogQuery.ToList();

            if (!result.Any())
            {
                _logger.LogError(
                    $"No catalog found for CatalogId: {request.CatalogId}");

                throw new NotFoundCustomException(
                    "Catalog not found.",
                    $"No active catalog found for CatalogId: {request.CatalogId}");
            }

            foreach (var catalog in result)
            {
                if (!catalog.CatalogId.HasValue)
                {
                    _logger.LogInfo(
                        $"Catalog ID is null for supplier ID: {catalog.SupplierId}");

                    throw new NotFoundCustomException(
                        "Catalog ID is null.",
                        $"Catalog ID is null for supplier ID: {catalog.SupplierId}");
                }

                var catalogAssetMappings =
                    _repository.CatalogAssetMapping
                        .FindByCondition(x =>
                            x.CatalogId == catalog.CatalogId.Value &&
                            x.IsActive)
                        .ToList();

                if (catalogAssetMappings.Any())
                {
                    var assetIds = catalogAssetMappings
                        .Select(x => x.AssetId)
                        .Distinct()
                        .ToList();

                    var assets = _repository.Asset
                        .FindByCondition(x =>
                            assetIds.Contains(x.Id) &&
                            x.IsActive)
                        .ToList();

                    catalog.Asset = assets
                        .Select(asset => new AssetDto
                        {
                            Id = asset.Id,
                            AssetType = asset.AssetType?.ToString(),
                            AssetName = asset.AssetName,
                            FileType = asset.FileType.ToString(),
                            FileName = asset.FileName
                        })
                        .ToList();
                }
                else
                {
                    catalog.Asset = new List<AssetDto>();
                }
            }

            _logger.LogInfo(
                $"Returned {result.Count} catalog record(s) for CatalogId: {request.CatalogId}");

            return await Task.FromResult(result);
        }
    }
}