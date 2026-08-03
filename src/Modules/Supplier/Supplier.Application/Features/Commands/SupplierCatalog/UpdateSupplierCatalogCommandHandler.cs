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
            _logger.LogInfo("Updating Supplier Catalog");
            var catalog = _repositoryWrapper.SupplierCatalog
                .FindFirstByCondition(x =>
                    x.Id == request.Catalog.Id &&
                    x.IsActive);

            if (catalog == null)
            {
                _logger.LogError("Supplier catalog not found.");
                throw new NotFoundCustomException(
                    "Supplier catalog not found.",
                    "Supplier catalog not found.");
            }
            if (string.IsNullOrWhiteSpace(request.Catalog.CatalogType))
            {
                _logger.LogError("Catalog Type is required.");
                throw new BadRequestCustomException(
                    "Catalog Type is required.",
                    "Catalog Type is required.");
            }
            _logger.LogInfo($"Validating Catalog Type: {request.Catalog.CatalogType}");
            var catalogType = request.Catalog.CatalogType.Trim().ToUpper();

            if (catalogType != "CATALOG" &&
                catalogType != "NON_CATALOG")
            {
                _logger.LogError($"Invalid Catalog Type: {catalogType}");
                throw new BadRequestCustomException(
                    "Invalid Catalog Type.",
                    "Catalog Type must be either CATALOG or NON_CATALOG.");
            }

            if (catalogType == Common.CATALOG)
            {
                _logger.LogInfo("Catalog Type is CATALOG. Validating price.");
                if (!request.Catalog.Price.HasValue)
                {
                    _logger.LogError("Price is required for Catalog items.");
                    throw new BadRequestCustomException(
                        "Price is required for Catalog items.",
                        "Price is required.");
                }

                request.Catalog.IsPunchOut = false;

            }
            else
            {
                if (!request.Catalog.Price.HasValue)
                {
                    _logger.LogInfo("Catalog Type is NON_CATALOG. Setting price to null.");
                    throw new BadRequestCustomException(
                        "Price is required for Catalog items.",
                        "Price is required.");
                }

                request.Catalog.IsPunchOut = false;

            }

            if (catalogType == Common.NON_CATALOG)
            {
                _logger.LogInfo("Catalog Type is NON_CATALOG. Validating punchout.");
                request.Catalog.Price = null;

                if (request.Catalog.IsPunchOut &&
                    string.IsNullOrWhiteSpace(request.Catalog.PunchOutUrl))
                {
                    _logger.LogError("PunchOut URL is required when IsPunchOut is true.");
                    throw new BadRequestCustomException(
                        "PunchOut URL is required.",
                        "PunchOut URL is required when IsPunchOut is true.");
                }
            }

            // Update catalog fields
            if (!string.IsNullOrWhiteSpace(request.Catalog.CatalogName))
            {
                _logger.LogInfo($"Updating Catalog Name to: {request.Catalog.CatalogName}");
                catalog.CatalogName = request.Catalog.CatalogName;
            }

            if (!string.IsNullOrWhiteSpace(request.Catalog.Description))
            {
                _logger.LogInfo($"Updating Catalog Description to: {request.Catalog.Description}");
                catalog.Description = request.Catalog.Description;
            }
            if (request.Catalog.Price.HasValue)
            {
                _logger.LogInfo($"Updating Catalog Price to: {request.Catalog.Price.Value}");
                catalog.Price = request.Catalog.Price.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Catalog.UnitOfMeasure))
            {
                _logger.LogInfo($"Updating Catalog Unit of Measure to: {request.Catalog.UnitOfMeasure}");
                catalog.UnitOfMeasure = request.Catalog.UnitOfMeasure;
            }
            if (request.Catalog.Segment.HasValue)
            {
                _logger.LogInfo($"Updating Catalog Segment to: {request.Catalog.Segment.Value}");
                catalog.Segment = request.Catalog.Segment.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Catalog.SegmentTitle))
            {
                _logger.LogInfo($"Updating Catalog Segment Title to: {request.Catalog.SegmentTitle}");
                catalog.SegmentTitle = request.Catalog.SegmentTitle;
            }
            if (request.Catalog.Family.HasValue)
            {
                _logger.LogInfo($"Updating Catalog Family to: {request.Catalog.Family.Value}");
                catalog.Family = request.Catalog.Family.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Catalog.FamilyTitle))
            {
                _logger.LogInfo($"Updating Catalog Family Title to: {request.Catalog.FamilyTitle}");
                catalog.FamilyTitle = request.Catalog.FamilyTitle;
            }
            if (request.Catalog.Commodity.HasValue)
            {
                _logger.LogInfo($"Updating Catalog Commodity to: {request.Catalog.Commodity.Value}");
                catalog.Commodity = request.Catalog.Commodity.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Catalog.CommodityTitle))
            {
                _logger.LogInfo($"Updating Catalog Commodity Title to: {request.Catalog.CommodityTitle}");
                catalog.CommodityTitle = request.Catalog.CommodityTitle;
            }
            if (request.Catalog.Class.HasValue)
            {
                _logger.LogInfo($"Updating Catalog Class to: {request.Catalog.Class.Value}");
                catalog.Class = request.Catalog.Class.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Catalog.ClassTitle))
            {
                _logger.LogInfo($"Updating Catalog Class Title to: {request.Catalog.ClassTitle}");
                catalog.ClassTitle = request.Catalog.ClassTitle;
            }
            if (!string.IsNullOrWhiteSpace(request.Catalog.CatalogType))
            {
                _logger.LogInfo($"Updating Catalog Type to: {request.Catalog.CatalogType}");

                catalog.CatalogType = request.Catalog.CatalogType;
            }
            catalog.IsPunchOut = request.Catalog.IsPunchOut;
            if (!string.IsNullOrWhiteSpace(request.Catalog.PunchOutUrl))
            {
                _logger.LogInfo($"Updating Catalog Punch-Out URL to: {request.Catalog.PunchOutUrl}");
                catalog.PunchOutUrl = request.Catalog.PunchOutUrl;
            }
            _repositoryWrapper.SupplierCatalog.Update(catalog);

            // Update assets only if assets are sent
            if (request.Catalog.Assets != null)
            {
                _logger.LogInfo($"Updating assets for catalog {catalog.Id}");
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
                    _logger.LogInfo($"Checking if asset {mapping.AssetId} is still present in the request for catalog {catalog.Id}");
                    if (!requestAssetIds.Contains(mapping.AssetId))
                    {
                        _logger.LogInfo($"Asset {mapping.AssetId} is not present in the request. Deactivating and removing mapping for catalog {catalog.Id}");
                        var existingAsset = _repositoryWrapper.Asset
                            .FindFirstByCondition(x => x.Id == mapping.AssetId);

                        if (existingAsset != null)
                        {
                            _logger.LogInfo($"Deactivating asset {existingAsset.Id}");
                            existingAsset.IsActive = false;
                            _repositoryWrapper.Asset.Update(existingAsset);
                        }

                        _repositoryWrapper.CatalogAssetMapping.Delete(mapping);
                    }
                }

                // Upload only newly added assets
                foreach (var asset in request.Catalog.Assets.Where(x => x.Id == Guid.Empty))
                {
                    _logger.LogInfo($"Uploading new asset for catalog {catalog.Id}");
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

                    _logger.LogInfo($"New asset uploaded for catalog {catalog.Id}. Asset ID: {assetId}");

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