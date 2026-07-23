using Supplier.Application.Features.Commands.Asset;
using MediatR;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class UpdateSupplierCatalogCommandHandler
        : IRequestHandler<UpdateSupplierCatalogCommand, bool>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public UpdateSupplierCatalogCommandHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger,
            IMediator mediator)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            UpdateSupplierCatalogCommand request,
            CancellationToken cancellationToken)
        {
            var catalog = _repositoryWrapper.SupplierCatalog
                .FindFirstByCondition(x =>
                    x.Id == request.Catalog.Id &&
                    x.IsActive);

            if (catalog == null)
                throw new NotFoundCustomException(
                    "Supplier catalog not found.",
                    "Supplier catalog not found.");

            // Update catalog fields
            if (!string.IsNullOrWhiteSpace(request.Catalog.CatalogName))
                catalog.CatalogName = request.Catalog.CatalogName;

            if (!string.IsNullOrWhiteSpace(request.Catalog.Description))
                catalog.Description = request.Catalog.Description;

            if (request.Catalog.Price.HasValue)
                catalog.Price = request.Catalog.Price.Value;

            if (!string.IsNullOrWhiteSpace(request.Catalog.UnitOfMeasure))
                catalog.UnitOfMeasure = request.Catalog.UnitOfMeasure;

            _repositoryWrapper.SupplierCatalog.Update(catalog);

            // Update assets only if assets are sent
            if (request.Catalog.Assets != null)
            {
                // Existing mappings
                var existingMappings = _repositoryWrapper.CatalogAssetMapping
                    .FindByCondition(x => x.CatalogId == catalog.Id)
                    .ToList();

                // Asset ids received from frontend (existing assets)
                var requestAssetIds = request.Catalog.Assets
                    .Where(x => x.Id != Guid.Empty)
                    .Select(x => x.Id)
                    .ToHashSet();

                // Remove assets not present in request
                foreach (var mapping in existingMappings)
                {
                    if (!requestAssetIds.Contains(mapping.AssetId))
                    {
                        var existingAsset = _repositoryWrapper.Asset
                            .FindFirstByCondition(x => x.Id == mapping.AssetId);

                        if (existingAsset != null)
                        {
                            existingAsset.IsActive = false;
                            _repositoryWrapper.Asset.Update(existingAsset);
                        }

                        _repositoryWrapper.CatalogAssetMapping.Delete(mapping);
                    }
                }

                // Upload only newly added assets
                foreach (var asset in request.Catalog.Assets.Where(x => x.Id == Guid.Empty))
                {
                    AssetUploadDto uploadDto = new()
                    {
                        EntityId = catalog.Id,
                        EntityType = asset.EntityType,
                        AssetType = asset.AssetType,
                        FileBytes = asset.FileBytes,
                        FileName = asset.FileName,
                        ContentType = asset.ContentType,
                        IsSingletonAsset = asset.IsSingletonAsset
                    };

                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(uploadDto),
                        cancellationToken);

                    _repositoryWrapper.CatalogAssetMapping.Create(
                        new CatalogAssetMapping
                        {
                            Id = Guid.NewGuid(),
                            CatalogId = catalog.Id,
                            AssetId = assetId
                        });
                }
            }

            _repositoryWrapper.Save();

            _logger.LogInfo($"Supplier catalog updated : {catalog.Id}");

            return true;
        }
    }
}