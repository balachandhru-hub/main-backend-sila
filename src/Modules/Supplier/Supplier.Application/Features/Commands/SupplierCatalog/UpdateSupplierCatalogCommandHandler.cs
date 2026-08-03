using Supplier.Application.Features.Commands.Asset;
using MediatR;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;

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
                     if (string.IsNullOrWhiteSpace(request.Catalog.CatalogType))
            {
                throw new BadRequestCustomException(
                    "Catalog Type is required.",
                    "Catalog Type is required.");
            }
              var catalogType = request.Catalog.CatalogType.Trim().ToUpper();

            if (catalogType != "CATALOG" &&
                catalogType != "NON_CATALOG")
            {
                throw new BadRequestCustomException(
                    "Invalid Catalog Type.",
                    "Catalog Type must be either CATALOG or NON_CATALOG.");
            }

            if (catalogType == Common.CATALOG)
            {
                if (!request.Catalog.Price.HasValue)
                {
                    throw new BadRequestCustomException(
                        "Price is required for Catalog items.",
                        "Price is required.");
                }

                request.Catalog.IsPunchOut = false;
                request.Catalog.PunchOutUrl = null;
            }
            else
            {
                if (!request.Catalog.Price.HasValue)
                {
                    throw new BadRequestCustomException(
                        "Price is required for Catalog items.",
                        "Price is required.");
                }

                request.Catalog.IsPunchOut = false;
                request.Catalog.PunchOutUrl = null;
            }

            if (catalogType == Common.NON_CATALOG)
            {
                request.Catalog.Price = null;

                if (request.Catalog.IsPunchOut &&
                    string.IsNullOrWhiteSpace(request.Catalog.PunchOutUrl))
                {
                    throw new BadRequestCustomException(
                        "PunchOut URL is required.",
                        "PunchOut URL is required when IsPunchOut is true.");
                }
            }

            // Update catalog fields
            if (!string.IsNullOrWhiteSpace(request.Catalog.CatalogName))
                catalog.CatalogName = request.Catalog.CatalogName;

            if (!string.IsNullOrWhiteSpace(request.Catalog.Description))
                catalog.Description = request.Catalog.Description;

            if (request.Catalog.Price.HasValue)
                catalog.Price = request.Catalog.Price.Value;

            if (!string.IsNullOrWhiteSpace(request.Catalog.UnitOfMeasure))
                catalog.UnitOfMeasure = request.Catalog.UnitOfMeasure;
            if (request.Catalog.Segment.HasValue)
                catalog.Segment = request.Catalog.Segment.Value;
            if (!string.IsNullOrWhiteSpace(request.Catalog.SegmentTitle))
                catalog.SegmentTitle = request.Catalog.SegmentTitle;
            if (request.Catalog.Family.HasValue)
                catalog.Family = request.Catalog.Family.Value;
            if (!string.IsNullOrWhiteSpace(request.Catalog.FamilyTitle))
                catalog.FamilyTitle = request.Catalog.FamilyTitle;
            if (request.Catalog.Commodity.HasValue)
                catalog.Commodity = request.Catalog.Commodity.Value;
            if (!string.IsNullOrWhiteSpace(request.Catalog.CommodityTitle))
                catalog.CommodityTitle = request.Catalog.CommodityTitle;
            if (request.Catalog.Class.HasValue)
                catalog.Class = request.Catalog.Class.Value;
            if (!string.IsNullOrWhiteSpace(request.Catalog.ClassTitle))
                catalog.ClassTitle = request.Catalog.ClassTitle;
            if (!string.IsNullOrWhiteSpace(request.Catalog.CatalogType))
                catalog.CatalogType = request.Catalog.CatalogType;
            catalog.IsPunchOut = request.Catalog.IsPunchOut;
            if (!string.IsNullOrWhiteSpace(request.Catalog.PunchOutUrl))
catalog.PunchOutUrl = request.Catalog.PunchOutUrl;
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