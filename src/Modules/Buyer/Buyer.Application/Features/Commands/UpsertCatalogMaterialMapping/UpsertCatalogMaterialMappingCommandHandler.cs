using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpsertCatalogMaterialMapping
{
    public class UpsertCatalogMaterialMappingCommandHandler : IRequestHandler<UpsertCatalogMaterialMappingCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public UpsertCatalogMaterialMappingCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<Guid> Handle(UpsertCatalogMaterialMappingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Saving catalog material mapping. CatalogId: {request.Request.CatalogId}, MaterialId: {request.Request.MaterialId}, UserId: {request.UserId}");

            if (request.Request.CatalogId == Guid.Empty || request.Request.MaterialId == Guid.Empty)
            {
                _logger.LogError("Catalog id or material id is missing.");
                throw new BadRequestCustomException("Product and material are required.", "Select a catalog product and an Item Master material.");
            }

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            ItemBuyerMaster? material = await _repository.ItemBuyerMaster.FindFirstByConditionAsync(
                x => x.Id == request.Request.MaterialId && x.BuyerId == buyer.Id && x.IsActive);
            if (material == null)
            {
                _logger.LogError($"Material not found. MaterialId: {request.Request.MaterialId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Material not found.", "Select an active Item Master material of this buyer organization.");
            }

            if (string.IsNullOrWhiteSpace(material.MaterialCode))
            {
                _logger.LogError($"Material has no material code. MaterialId: {material.Id}");
                throw new BadRequestCustomException("Material has no material code.", "Select an Item Master material that has a material code.");
            }

            // The product must exist in the supplier catalog; its sku is stored with the mapping.
            List<BuyerCatalogItemDto> products = await _supplierApiClient.GetBuyerCatalogStock(
                new List<Guid> { request.Request.CatalogId }, cancellationToken);
            BuyerCatalogItemDto? product = products.FirstOrDefault(x => x.CatalogId == request.Request.CatalogId);
            if (product == null)
            {
                _logger.LogError($"Product not found in the product catalog. CatalogId: {request.Request.CatalogId}");
                throw new NotFoundCustomException("Product not found.", "Select a product from the product catalog.");
            }

            List<Guid> otherCatalogIds = await _repository.CatalogMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id
                                      && x.MaterialId == material.Id
                                      && x.CatalogId != request.Request.CatalogId
                                      && x.IsActive)
                .Select(x => x.CatalogId)
                .ToListAsync(cancellationToken);
            if (otherCatalogIds.Count > 0)
            {
                List<BuyerCatalogItemDto> otherProducts = await _supplierApiClient.GetBuyerCatalogStock(otherCatalogIds, cancellationToken);
                BuyerCatalogItemDto? sameSupplierProduct = otherProducts.FirstOrDefault(x => x.SupplierId == product.SupplierId);
                if (sameSupplierProduct != null)
                {
                    _logger.LogError(
                        $"Material is already mapped to another product of the supplier. MaterialId: {material.Id}, SupplierId: {product.SupplierId}, CatalogId: {sameSupplierProduct.CatalogId}");
                    throw new BadRequestCustomException(
                        "Material is already mapped for this supplier.",
                        $"{material.MaterialCode} is mapped to '{sameSupplierProduct.CatalogName ?? sameSupplierProduct.Sku}' of {product.SupplierName ?? "this supplier"}. Select another material, or change the mapping of that product first.");
                }
            }

            CatalogMaterialMapping? mapping = await _repository.CatalogMaterialMapping.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.CatalogId == request.Request.CatalogId);
            if (mapping == null)
            {
                mapping = new CatalogMaterialMapping
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    CatalogId = request.Request.CatalogId,
                    Sku = product.Sku,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode
                };
                _repository.CatalogMaterialMapping.Create(mapping);
            }
            else
            {
                mapping.Sku = product.Sku;
                mapping.MaterialId = material.Id;
                mapping.MaterialCode = material.MaterialCode;
                mapping.IsActive = true;
                _repository.CatalogMaterialMapping.Update(mapping);
            }

            // Lines already requested for this product in a bucket that is still OPEN take the material. Frozen buckets are not touched.
            List<Guid> openBucketIds = await _repository.WeeklyBucket
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Status == Common.WEEKLY_BUCKET_OPEN && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            List<WeeklyBucketItem> openLines = await _repository.WeeklyBucketItem
                .FindByCondition(x => openBucketIds.Contains(x.WeeklyBucketId) && x.CatalogId == request.Request.CatalogId && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (WeeklyBucketItem line in openLines)
            {
                line.MaterialId = material.Id;
                line.MaterialCode = material.MaterialCode;
                _repository.WeeklyBucketItem.Update(line);
            }

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Catalog material mapping saved. MappingId: {mapping.Id}, CatalogId: {mapping.CatalogId}, MaterialCode: {mapping.MaterialCode}, OpenLinesUpdated: {openLines.Count}");
            return mapping.Id;
        }
    }
}
