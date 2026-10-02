using Buyer.Application.Contracts;
using Buyer.Application.Features.Shared;
using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.AddWeeklyBucketItems
{
    public class AddWeeklyBucketItemsCommandHandler : IRequestHandler<AddWeeklyBucketItemsCommand, WeeklyBucketAddItemsResultDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IStockInHandProvider _stockInHandProvider;
        private readonly IConfiguration _configuration;

        public AddWeeklyBucketItemsCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient,
            IStockInHandProvider stockInHandProvider,
            IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
            _stockInHandProvider = stockInHandProvider;
            _configuration = configuration;
        }

        public async Task<WeeklyBucketAddItemsResultDto> Handle(AddWeeklyBucketItemsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Adding items to the weekly bucket. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}, OutletId: {request.Request.OutletId}");

            List<CatalogItemWriteDto> requested = request.Request.Items ?? new List<CatalogItemWriteDto>();
            if (requested.Count == 0)
            {
                _logger.LogError($"No products were sent. UserId: {request.UserId}");
                throw new BadRequestCustomException("No products to add.", "Select at least one product from the product catalog.");
            }

            if (requested.Any(line => line.CatalogId == Guid.Empty))
            {
                _logger.LogError($"A product without a catalog id was sent. UserId: {request.UserId}");
                throw new BadRequestCustomException("Product is required.", "Select every product from the product catalog.");
            }

            if (requested.Any(line => line.Quantity <= 0))
            {
                _logger.LogError($"A quantity is not greater than zero. UserId: {request.UserId}");
                throw new BadRequestCustomException("Quantity must be greater than zero.", "Enter a quantity for every product.");
            }

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            BuyerOutlet outlet = await WeeklyBucketRules.ResolveOutletAsync(
                _repository, _logger, buyer.Id, request.UserId, request.Request.OutletId, cancellationToken);
            BuyerProperty property = await WeeklyBucketRules.GetPropertyAsync(
                _repository, _logger, outlet.PropertyId!.Value, buyer.Id, cancellationToken);

            // The active bucket of the property. It is created the first time somebody requests a product for the week.
            (WeeklyBucket? bucket, int year, int weekNumber) = await WeeklyBucketRules.FindActiveBucketAsync(
                _repository, _configuration, buyer.Id, property.Id, cancellationToken);
            List<WeeklyBucketItem> existingLines = new List<WeeklyBucketItem>();
            if (bucket == null)
            {
                bucket = new WeeklyBucket
                {
                    Id = Guid.NewGuid(),
                    BuyerOrganizationId = request.OrganizationId,
                    BuyerId = buyer.Id,
                    BucketCode = WeeklyBucketRules.BuildBucketCode(weekNumber, property.PlantCode),
                    WeekNumber = weekNumber,
                    Year = year,
                    PropertyId = property.Id,
                    PlantCode = property.PlantCode,
                    CompanyCode = property.CompanyCode,
                    Status = Common.WEEKLY_BUCKET_OPEN
                };
                _repository.WeeklyBucket.Create(bucket);
                WeeklyBucketRules.AddAudit(
                    _repository, bucket.Id, request.UserId, Common.AUDIT_CREATED, $"Weekly bucket {bucket.BucketCode} created.");
            }
            else
            {
                WeeklyBucketRules.EnsureOpen(bucket, _logger);
                existingLines = await _repository.WeeklyBucket.GetItemsAsync(bucket.Id, cancellationToken);
            }

            // The same product sent twice in one request is one quantity.
            Dictionary<Guid, decimal> quantities = requested
                .GroupBy(line => line.CatalogId)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));
            List<Guid> catalogIds = quantities.Keys.ToList();

            // Name, supplier, unit, price and stock are read from the Supplier service, never from the request.
            List<BuyerCatalogItemDto> products = await _supplierApiClient.GetBuyerCatalogStock(catalogIds, cancellationToken);
            List<CatalogMaterialMapping> mappings = await _repository.CatalogMaterialMapping
                .FindByCondition(x => x.BuyerId == buyer.Id && catalogIds.Contains(x.CatalogId) && x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (KeyValuePair<Guid, decimal> line in quantities)
            {
                BuyerCatalogItemDto? product = products.FirstOrDefault(x => x.CatalogId == line.Key);
                if (product == null)
                {
                    _logger.LogError($"Product not found in the product catalog. CatalogId: {line.Key}");
                    throw new NotFoundCustomException("Product not found.", "Select a product from the product catalog.");
                }

                // Lines of different requestors are never merged. The same requestor, product and outlet is one line.
                WeeklyBucketItem? existing = existingLines.FirstOrDefault(
                    x => x.RequestorUserId == request.UserId && x.CatalogId == line.Key && x.OutletId == outlet.Id);
                if (existing != null)
                {
                    decimal previousQuantity = existing.RequestedQuantity;
                    existing.RequestedQuantity += line.Value;
                    existing.ApprovedQuantity += line.Value;
                    existing.AvailabilityStatus = WeeklyBucketRules.GetAvailability(existing.SupplierStock, existing.ApprovedQuantity);
                    WeeklyBucketRules.AddAudit(
                        _repository,
                        bucket.Id,
                        request.UserId,
                        Common.AUDIT_QUANTITY_CHANGED,
                        $"Product={existing.ProductName} Outlet={outlet.OutletName} RequestedQuantity: {previousQuantity} -> {existing.RequestedQuantity}");
                    continue;
                }

                CatalogMaterialMapping? mapping = mappings.FirstOrDefault(x => x.CatalogId == line.Key);
                decimal? stockInHand = await _stockInHandProvider.GetStockInHandAsync(
                    buyer.Id, outlet.StorageLocation, mapping?.MaterialCode, cancellationToken);

                WeeklyBucketItem item = new WeeklyBucketItem
                {
                    Id = Guid.NewGuid(),
                    WeeklyBucketId = bucket.Id,
                    CatalogId = line.Key,
                    Sku = product.Sku,
                    ProductName = product.CatalogName ?? product.Description ?? string.Empty,
                    Description = product.Description,
                    MaterialId = mapping?.MaterialId,
                    MaterialCode = mapping?.MaterialCode,
                    SupplierId = product.SupplierId,
                    SupplierName = product.SupplierName,
                    UnitOfMeasure = product.UnitOfMeasure,
                    Price = product.Price,
                    Currency = product.Currency,
                    DiscountPercent = product.DiscountPercent,
                    RequestedQuantity = line.Value,
                    ApprovedQuantity = line.Value,
                    SupplierStock = product.AvailableStock,
                    SupplierStockRefreshedOn = DateTime.UtcNow,
                    StockInHand = stockInHand,
                    AvailabilityStatus = WeeklyBucketRules.GetAvailability(product.AvailableStock, line.Value),
                    LineStatus = Common.LINE_REQUESTED,
                    RequestorUserId = request.UserId,
                    OutletId = outlet.Id,
                    StorageLocation = outlet.StorageLocation
                };
                _repository.WeeklyBucketItem.Create(item);
                WeeklyBucketRules.AddAudit(
                    _repository,
                    bucket.Id,
                    request.UserId,
                    Common.AUDIT_ITEM_ADDED,
                    $"Product={item.ProductName} Outlet={outlet.OutletName} RequestedQuantity={item.RequestedQuantity}");
            }

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Items added to the weekly bucket. WeeklyBucketId: {bucket.Id}, BucketCode: {bucket.BucketCode}, Products: {quantities.Count}, UserId: {request.UserId}");
            return new WeeklyBucketAddItemsResultDto
            {
                BucketId = bucket.Id,
                BucketCode = bucket.BucketCode
            };
        }

        private BuyerBusinessProfile GetBuyer(Guid organizationId)
        {
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            return buyer;
        }
    }
}
