using MediatR;
using Microsoft.EntityFrameworkCore;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;
using SharedKernel.Dto;

namespace Supplier.Application.Features.Queries.SupplierCatalog
{
    public class GetAllSupplierCatalogQueryHandler
        : IRequestHandler<GetAllSupplierCatalogQuery, List<GetSupplierCatalogDto>>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public GetAllSupplierCatalogQueryHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
        }

        public async Task<List<GetSupplierCatalogDto>> Handle(
            GetAllSupplierCatalogQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Getting Supplier Catalogs");

            var catalogs = await _repositoryWrapper.SupplierCatalog
                .FindByCondition(x => x.IsActive)
                .ToListAsync(cancellationToken);

            List<GetSupplierCatalogDto> result = new();

            foreach (var catalog in catalogs)
            {
                var dto = new GetSupplierCatalogDto
                {
                    Id = catalog.Id,
                    CatalogName = catalog.CatalogName,
                    Description = catalog.Description,
                    Price = catalog.Price,
                    UnitOfMeasure = catalog.UnitOfMeasure,
                    Assets = new List<AssetDto>()
                };

                var mappings = _repositoryWrapper.CatalogAssetMapping
                    .FindByCondition(x => x.CatalogId == catalog.Id)
                    .ToList();

                foreach (var mapping in mappings)
                {
                    var asset = _repositoryWrapper.Asset
                        .FindFirstByCondition(x =>
                            x.Id == mapping.AssetId &&
                            x.IsActive);

                    if (asset == null)
                        continue;

                    dto.Assets.Add(new AssetDto
                    {
                        Id = asset.Id,
                        FileName = asset.FileName,
                        AssetName = asset.AssetName
                    });
                }

                result.Add(dto);
            }

            return result;
        }
    }
}