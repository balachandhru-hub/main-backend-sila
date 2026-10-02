using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services
{
    /// <summary>
    /// Sends the purchase orders of an approved weekly bucket to the buyer's active purchase order API:
    /// one purchase order per supplier. Called from the final approval request and from the retry request.
    /// A purchase order that already succeeded is never sent again.
    /// </summary>
    public class WeeklyBucketIntegrationProcessor
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IBuyerPurchaseDocumentGateway _buyerGateway;
        private readonly IUserContext _userContext;
        private readonly ILoggerManager _logger;

        public WeeklyBucketIntegrationProcessor(
            IRepositoryWrapper repository,
            IBuyerPurchaseDocumentGateway buyerGateway,
            IUserContext userContext,
            ILoggerManager logger)
        {
            _repository = repository;
            _buyerGateway = buyerGateway;
            _userContext = userContext;
            _logger = logger;
        }

        public async Task SendPurchaseOrdersAsync(Guid weeklyBucketId, Guid actorUserId, CancellationToken cancellationToken)
        {
            _userContext.SetCurrentUserId(actorUserId);
            WeeklyBucket? bucket = await _repository.WeeklyBucket.GetTrackedByIdAsync(weeklyBucketId, cancellationToken);
            if (bucket == null)
            {
                _logger.LogError($"Weekly bucket not found while sending purchase orders. WeeklyBucketId: {weeklyBucketId}");
                return;
            }

            IntegrationApiDto? configuration = await _buyerGateway.FindPurchaseOrderApiAsync(
                bucket.BuyerOrganizationId,
                bucket.CompanyCode,
                cancellationToken);
            if (configuration == null)
            {
                bucket.Status = Common.WEEKLY_BUCKET_PO_FAILED;
                bucket.LastError = "No purchase order API is configured. Add a Purchase order API under Integrations, test it and activate it.";
                AddAudit(bucket, Common.AUDIT_ERP_FAILED, bucket.LastError);
                await _repository.SaveAsync();
                _logger.LogError($"No purchase order API is active. WeeklyBucketId: {bucket.Id}, BuyerId: {bucket.BuyerId}");
                return;
            }

            List<WeeklyBucketItem> lines = (await _repository.WeeklyBucket.GetItemsAsync(bucket.Id, cancellationToken))
                .Where(x => x.LineStatus != Common.LINE_EXCLUDED && x.ApprovedQuantity > 0)
                .ToList();
            if (lines.Count == 0)
            {
                bucket.Status = Common.WEEKLY_BUCKET_PO_FAILED;
                bucket.LastError = "The weekly bucket has no line to order.";
                AddAudit(bucket, Common.AUDIT_ERP_FAILED, bucket.LastError);
                await _repository.SaveAsync();
                _logger.LogError($"Weekly bucket has no line to order. WeeklyBucketId: {bucket.Id}");
                return;
            }

            List<BuyerOutlet> outlets = await _repository.WeeklyBucket.ListOutletsAsync(bucket.BuyerId, cancellationToken);
            List<PurchaseDocumentIntegration> existing = await _repository.WeeklyBucket.GetIntegrationsAsync(bucket.Id, cancellationToken);

            // One purchase order per supplier. A supplier that fails does not stop the others.
            List<string> errors = new List<string>();
            foreach (IGrouping<Guid, WeeklyBucketItem> supplierLines in lines.GroupBy(x => x.SupplierId))
            {
                string? error = await SendOneAsync(
                    bucket, configuration, supplierLines.Key, supplierLines.ToList(), outlets, existing, cancellationToken);
                if (error != null)
                {
                    errors.Add(error);
                }
            }

            if (errors.Count > 0)
            {
                bucket.Status = Common.WEEKLY_BUCKET_PO_FAILED;
                bucket.LastError = string.Join(" | ", errors);
                await _repository.SaveAsync();
                _logger.LogError($"Purchase order send failed. WeeklyBucketId: {bucket.Id}, System: {configuration.SystemName}, FailedSuppliers: {errors.Count}");
                return;
            }

            bucket.Status = Common.WEEKLY_BUCKET_PO_CREATED;
            bucket.LastError = null;
            await _repository.SaveAsync();
            _logger.LogInfo($"Purchase orders sent. WeeklyBucketId: {bucket.Id}, System: {configuration.SystemName}");
        }

        // Returns null when the supplier's purchase order exists, otherwise the error to show on the bucket.
        private async Task<string?> SendOneAsync(
            WeeklyBucket bucket,
            IntegrationApiDto configuration,
            Guid supplierId,
            List<WeeklyBucketItem> lines,
            List<BuyerOutlet> outlets,
            List<PurchaseDocumentIntegration> existing,
            CancellationToken cancellationToken)
        {
            string? supplierName = lines[0].SupplierName;
            PurchaseDocumentIntegration integration = await EnsureIntegrationAsync(bucket, configuration, supplierId, existing);
            if (integration.Status == Common.INTEGRATION_SUCCEEDED && !string.IsNullOrWhiteSpace(integration.ExternalDocumentNumber))
            {
                _logger.LogInfo($"Purchase order already exists. WeeklyBucketId: {bucket.Id}, SupplierId: {supplierId}, Document: {integration.ExternalDocumentNumber}");
                return null;
            }

            string correlationId = Guid.NewGuid().ToString("N");
            integration.Status = Common.INTEGRATION_PROCESSING;
            integration.CorrelationId = correlationId;
            integration.LastAttemptOn = DateTime.UtcNow;
            integration.DocumentType = Common.ERP_DOCUMENT_PO;
            AddAudit(bucket, Common.AUDIT_ERP_STARTED, $"Supplier={supplierName} System={configuration.SystemName} ConfigurationId={configuration.ConfigurationId} CorrelationId={correlationId}");
            await _repository.SaveAsync();

            // The ship-to of the outlet is sent only when every line of this supplier is for the same outlet.
            List<Guid> outletIds = lines.Select(x => x.OutletId).Distinct().ToList();
            BuyerOutlet? outlet = outletIds.Count == 1 ? outlets.FirstOrDefault(x => x.Id == outletIds[0]) : null;

            BuyerPurchaseDocumentRequest request = new BuyerPurchaseDocumentRequest
            {
                IdempotencyKey = integration.IdempotencyKey,
                WeeklyBucketId = bucket.Id,
                BucketCode = bucket.BucketCode,
                BuyerOrganizationId = bucket.BuyerOrganizationId,
                CompanyCode = bucket.CompanyCode,
                PlantCode = bucket.PlantCode,
                SupplierId = supplierId,
                SupplierName = supplierName,
                DocumentType = integration.DocumentType ?? Common.ERP_DOCUMENT_PO,
                ShipTo = outlet?.ExternalShipTo,
                OutletCode = outlet?.OutletCode,
                OutletName = outlet?.OutletName,
                Currency = lines[0].Currency,
                CorrelationId = correlationId,
                Lines = lines.Select(line => new BuyerPurchaseLine
                {
                    MaterialCode = line.MaterialCode ?? string.Empty,
                    MaterialName = line.ProductName,
                    Sku = line.Sku,
                    Quantity = line.ApprovedQuantity,
                    UnitOfMeasure = line.UnitOfMeasure,
                    UnitPrice = line.Price,
                    Currency = line.Currency,
                    StorageLocation = line.StorageLocation
                }).ToList()
            };

            ExternalCallResult result = await _buyerGateway.CreateBuyerPurchaseDocumentAsync(
                configuration,
                request,
                cancellationToken);

            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.DocumentNumber))
            {
                integration.Status = Common.INTEGRATION_SUCCEEDED;
                integration.ExternalDocumentNumber = result.DocumentNumber;
                integration.ResponseBody = result.ResponseBody;
                integration.ErrorMessage = null;
                integration.OutcomeUnknown = false;
                integration.NextAttemptOn = null;
                AddPurchaseOrder(bucket, configuration, supplierId, supplierName, result.DocumentNumber, lines);
                AddAudit(bucket, Common.AUDIT_ERP_SUCCEEDED, $"Supplier={supplierName} System={configuration.SystemName} DocumentNumber={result.DocumentNumber} CorrelationId={correlationId}");
                await _repository.SaveAsync();
                _logger.LogInfo(
                    $"Purchase order stored. WeeklyBucketId={bucket.Id} SupplierId={supplierId} System={configuration.SystemName} ConfigurationId={configuration.ConfigurationId} " +
                    $"CorrelationId={correlationId} DurationMs={result.DurationMs}");
                return null;
            }

            string error = result.ErrorMessage ?? "Purchase order creation failed.";
            integration.Status = result.OutcomeUnknown ? Common.INTEGRATION_UNKNOWN : Common.INTEGRATION_FAILED;
            integration.RetryCount += 1;
            integration.ErrorMessage = error;
            integration.OutcomeUnknown = result.OutcomeUnknown;
            integration.ResponseBody = result.ResponseBody;
            integration.LastAttemptOn = DateTime.UtcNow;
            integration.NextAttemptOn = null;
            AddAudit(bucket, Common.AUDIT_ERP_FAILED, $"Supplier={supplierName} System={configuration.SystemName} StatusCode={result.StatusCode} {error}");
            await _repository.SaveAsync();
            _logger.LogError(
                $"Purchase order call failed. WeeklyBucketId={bucket.Id} SupplierId={supplierId} System={configuration.SystemName} ConfigurationId={configuration.ConfigurationId} " +
                $"CorrelationId={correlationId} StatusCode={result.StatusCode}");
            return $"{supplierName ?? supplierId.ToString()}: {error}";
        }

        // One integration row per supplier of the bucket. The row carries the idempotency key, so a retry reuses it.
        private async Task<PurchaseDocumentIntegration> EnsureIntegrationAsync(
            WeeklyBucket bucket,
            IntegrationApiDto configuration,
            Guid supplierId,
            List<PurchaseDocumentIntegration> existing)
        {
            PurchaseDocumentIntegration? integration = existing.FirstOrDefault(
                x => x.IntegrationType == Common.ERP_OPERATION_PO_CREATE && x.SupplierOrganizationId == supplierId);
            if (integration != null)
            {
                return integration;
            }

            integration = new PurchaseDocumentIntegration
            {
                Id = Guid.NewGuid(),
                WeeklyBucketId = bucket.Id,
                BuyerOrganizationId = bucket.BuyerOrganizationId,
                SupplierOrganizationId = supplierId,
                IntegrationType = Common.ERP_OPERATION_PO_CREATE,
                IdempotencyKey = $"{bucket.Id}:{supplierId}",
                ConfigurationId = configuration.ConfigurationId!.Value,
                ConfigurationVersion = 1,
                ResolvedBaseUrl = configuration.BaseUrl ?? string.Empty,
                ResolvedPath = configuration.ResourcePath ?? string.Empty,
                ResolvedHttpMethod = configuration.HttpMethod ?? "POST",
                ResolvedErpType = configuration.SystemName ?? string.Empty,
                DocumentType = Common.ERP_DOCUMENT_PO,
                Status = Common.INTEGRATION_PENDING,
                IsActive = true
            };
            _repository.PurchaseDocumentIntegration.Create(integration);
            existing.Add(integration);
            await _repository.SaveAsync();
            return integration;
        }

        // The purchase order as it was sent: one header for the supplier and one line per bucket line.
        private void AddPurchaseOrder(
            WeeklyBucket bucket,
            IntegrationApiDto configuration,
            Guid supplierId,
            string? supplierName,
            string documentNumber,
            List<WeeklyBucketItem> lines)
        {
            PurchaseOrder purchaseOrder = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                BuyerId = bucket.BuyerId,
                BuyerOrganizationId = bucket.BuyerOrganizationId,
                PoNumber = documentNumber,
                SupplierId = supplierId,
                SupplierName = supplierName,
                SourceType = Common.PURCHASE_ORDER_SOURCE_WEEKLY_BUCKET,
                WeeklyBucketId = bucket.Id,
                BucketCode = bucket.BucketCode,
                CompanyCode = bucket.CompanyCode,
                PlantCode = bucket.PlantCode,
                Currency = lines[0].Currency,
                Status = Common.PURCHASE_ORDER_CREATED,
                OrderDate = DateTime.UtcNow,
                SourceSystem = configuration.SystemName
            };

            int lineNumber = 0;
            foreach (WeeklyBucketItem line in lines)
            {
                lineNumber += 1;
                decimal price = line.Price ?? 0;
                decimal discount = line.DiscountPercent ?? 0;
                decimal lineAmount = Math.Round(line.ApprovedQuantity * price * (100 - discount) / 100, 4);
                _repository.PurchaseOrderItem.Create(new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = purchaseOrder.Id,
                    LineNumber = lineNumber,
                    CatalogId = line.CatalogId,
                    Sku = line.Sku,
                    MaterialCode = line.MaterialCode,
                    ProductName = line.ProductName,
                    Quantity = line.ApprovedQuantity,
                    UnitOfMeasure = line.UnitOfMeasure,
                    UnitPrice = line.Price,
                    DiscountPercent = line.DiscountPercent,
                    LineAmount = lineAmount,
                    Currency = line.Currency,
                    OutletId = line.OutletId,
                    StorageLocation = line.StorageLocation
                });
                purchaseOrder.TotalAmount += lineAmount;
            }

            _repository.PurchaseOrder.Create(purchaseOrder);
        }

        private void AddAudit(WeeklyBucket bucket, string action, string? detail)
        {
            _repository.WeeklyBucketAudit.Create(new WeeklyBucketAudit
            {
                Id = Guid.NewGuid(),
                WeeklyBucketId = bucket.Id,
                Action = action,
                Detail = detail,
                ActorUserId = _userContext.GetCurrentUserId()
            });
        }
    }
}
