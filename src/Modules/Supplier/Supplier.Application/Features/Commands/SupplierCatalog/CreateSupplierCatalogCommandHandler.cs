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


            SupplierCatalogEntity catalog = new()
            {
                Id = Guid.NewGuid(),
                SupplierId = supplier.Id,
                CatalogName = request.Catalog.CatalogName,
                Description = request.Catalog.Description,
                Price = request.Catalog.Price,
                UnitOfMeasure = request.Catalog.UnitOfMeasure
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
                        EntityType = "SUPPLIER",
                        AssetType = "SUPPLIER_CATALOG",
                        FileBytes = asset.FileBytes,
                        FileName = asset.FileName,
                        ContentType = asset.ContentType,
                        IsSingletonAsset = false
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