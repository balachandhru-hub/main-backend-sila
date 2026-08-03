using Supplier.Application.Features.Commands.Asset;
using MediatR;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using SupplierCatalogEntity = Supplier.Domain.Entities.SupplierCatalog;
using Supplier.Domain.Common;

namespace Supplier.Application.Features.Commands.SupplierCatalog

{
    public class CreateSupplierCatalogCommandHandler
        : IRequestHandler<CreateSupplierCatalogCommand, Guid>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public CreateSupplierCatalogCommandHandler(
            IRepositoryWrapper repositoryWrapper,
            ILoggerManager logger,
            IMediator mediator)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<Guid> Handle(
            CreateSupplierCatalogCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Creating Supplier Catalog");

            var supplier =
                _repositoryWrapper.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId
                    && x.IsActive);

            if (supplier == null)
                throw new NotFoundCustomException(
                    "Supplier profile not found.",
                    "Supplier profile not found.");
            if (!string.Equals(
        supplier.Status,
        Common.VERIFIED_STATUS,
        StringComparison.OrdinalIgnoreCase))
            {
                throw new PreConditionFailedCustomException(
                    "Supplier must be verified before creating catalog.",
                    "Supplier is not verified.");
            }
              if (string.IsNullOrWhiteSpace(request.Catalog.CatalogType))
            {
                throw new BadRequestCustomException(
                    "Catalog Type is required.",
                    "Catalog Type is required.");
            }
              var catalogType = request.Catalog.CatalogType.Trim().ToUpper();
                if (catalogType != Common.CATALOG &&
                catalogType != Common.NON_CATALOG)
            {
                throw new BadRequestCustomException(
                    "Invalid Catalog Type.",
                    "Catalog Type must be either CATALOG or NON_CATALOG.");
            }
                if (catalogType == Common.CATALOG)
            {
                if (request.Catalog.Price == null)
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

            SupplierCatalogEntity catalog = new()
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                CatalogName = request.Catalog.CatalogName,
                Description = request.Catalog.Description,
                Price = request.Catalog.Price,
                UnitOfMeasure = request.Catalog.UnitOfMeasure,
                   Segment = request.Catalog.Segment,
                SegmentTitle = request.Catalog.SegmentTitle,

                Family = request.Catalog.Family,
                FamilyTitle = request.Catalog.FamilyTitle,

                Commodity = request.Catalog.Commodity,
                CommodityTitle = request.Catalog.CommodityTitle,

                Class = request.Catalog.Class,
                ClassTitle = request.Catalog.ClassTitle,

                CatalogType = catalogType,

                IsPunchOut = request.Catalog.IsPunchOut,

                PunchOutUrl = request.Catalog.PunchOutUrl
            };

            _repositoryWrapper.SupplierCatalog.Create(catalog);
            _repositoryWrapper.Save();

            if (request.Catalog.Assets != null &&
                request.Catalog.Assets.Any())
            {
                foreach (var asset in request.Catalog.Assets)
                {
                    AssetUploadDto uploadDto = new()
                    {
                        EntityId = catalog.Id,
                        EntityType = asset.EntityType,
                        AssetType = asset.AssetType,
                        FileName = asset.FileName,
                        ContentType = asset.ContentType,
                        IsSingletonAsset = false,
                        FileBytes = asset.FileBytes
                    };

                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(uploadDto));

                    CatalogAssetMapping mapping = new()
                    {
                        Id = Guid.NewGuid(),
                        CatalogId = catalog.Id,
                        AssetId = assetId
                    };

                    _repositoryWrapper.CatalogAssetMapping.Create(mapping);
                }

                _repositoryWrapper.Save();
            }

            _logger.LogInfo($"Supplier Catalog Created : {catalog.Id}");

            return catalog.Id;
        }
    }
}