using System.Text.Json;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services.Integration
{
    public interface IBuyerPurchaseDocumentGateway
    {
        /// <summary>
        /// The buyer organization's active purchase order API, or null when it has none.
        /// </summary>
        Task<IntegrationApiDto?> FindPurchaseOrderApiAsync(
            Guid buyerOrganizationId,
            string? companyCode,
            CancellationToken cancellationToken);

        Task<ExternalCallResult> CreateBuyerPurchaseDocumentAsync(
            IntegrationApiDto api,
            BuyerPurchaseDocumentRequest request,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Sends a purchase document to the buyer's purchase order API. The API is configured under
    /// Integrations, like every other API type; this class builds the document and the integration
    /// service makes the call with the saved sign-in.
    /// Weekly bucket code calls CreateBuyerPurchaseDocument, not a vendor-specific method.
    /// </summary>
    public class BuyerPurchaseDocumentGateway : IBuyerPurchaseDocumentGateway
    {
        private readonly IOperationsIntegrationClient _integrationClient;
        private readonly ILoggerManager _logger;

        public BuyerPurchaseDocumentGateway(IOperationsIntegrationClient integrationClient, ILoggerManager logger)
        {
            _integrationClient = integrationClient;
            _logger = logger;
        }

        public async Task<IntegrationApiDto?> FindPurchaseOrderApiAsync(
            Guid buyerOrganizationId,
            string? companyCode,
            CancellationToken cancellationToken)
        {
            IntegrationApiDto api = await _integrationClient.ResolveAsync(
                buyerOrganizationId, Common.INTEGRATION_PROCESS_POST_PO, companyCode, cancellationToken);
            return api.Configured && api.ConfigurationId != null ? api : null;
        }

        public async Task<ExternalCallResult> CreateBuyerPurchaseDocumentAsync(
            IntegrationApiDto api,
            BuyerPurchaseDocumentRequest request,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                [Common.IDEMPOTENCY_HEADER] = request.IdempotencyKey,
                ["X-Correlation-Id"] = request.CorrelationId
            };

            IntegrationSendResultDto sent = await _integrationClient.SendAsync(
                request.BuyerOrganizationId,
                api.ConfigurationId!.Value,
                BuildPayload(api, request),
                headers,
                cancellationToken);

            bool httpSuccess = sent.Answered && sent.StatusCode >= 200 && sent.StatusCode < 300;
            string? documentNumber = httpSuccess ? ExternalDocumentNumberReader.TryRead(sent.ResponseBody ?? string.Empty) : null;
            bool succeeded = httpSuccess && !string.IsNullOrWhiteSpace(documentNumber);

            if (succeeded)
            {
                _logger.LogInfo(
                    $"Buyer ERP call finished. WeeklyBucketId={request.WeeklyBucketId} SupplierId={request.SupplierId} BuyerOrganizationId={request.BuyerOrganizationId} " +
                    $"IntegrationType={Common.INTEGRATION_BUYER_ERP} ConfigurationId={api.ConfigurationId} CorrelationId={request.CorrelationId} " +
                    $"StatusCode={sent.StatusCode} DurationMs={sent.DurationMs}");
            }
            else
            {
                _logger.LogError(
                    $"Buyer ERP call did not create a document. WeeklyBucketId={request.WeeklyBucketId} SupplierId={request.SupplierId} " +
                    $"ConfigurationId={api.ConfigurationId} CorrelationId={request.CorrelationId} StatusCode={sent.StatusCode} Code={sent.ErrorCode} DurationMs={sent.DurationMs}");
            }

            return new ExternalCallResult
            {
                Succeeded = succeeded,
                StatusCode = sent.StatusCode,
                DocumentNumber = documentNumber,
                ResponseBody = sent.ResponseBody,

                // A success without a document number, like a call that broke off, may have created the
                // document: neither is sent again automatically.
                OutcomeUnknown = sent.OutcomeUnknown || (httpSuccess && string.IsNullOrWhiteSpace(documentNumber)),
                ErrorMessage = succeeded
                    ? null
                    : httpSuccess
                        ? "Buyer ERP responded successfully but did not include a document number."
                        : !sent.Answered && sent.OutcomeUnknown
                            ? "Buyer ERP call did not complete. The document may already exist, so this attempt is not retried automatically."
                            : sent.Answered
                                ? $"Buyer ERP returned HTTP {sent.StatusCode}."
                                : sent.ErrorMessage ?? "Buyer ERP call failed.",
                DurationMs = sent.DurationMs
            };
        }

        private static string BuildPayload(IntegrationApiDto api, BuyerPurchaseDocumentRequest request)
        {
            if (!string.IsNullOrWhiteSpace(api.RequestBody))
            {
                return ApplyConfiguredBody(api.RequestBody, request);
            }

            return JsonSerializer.Serialize(new
            {
                documentType = request.DocumentType,
                externalReference = request.WeeklyBucketId,
                bucketCode = request.BucketCode,
                idempotencyKey = request.IdempotencyKey,
                buyerOrganizationId = request.BuyerOrganizationId,
                companyCode = request.CompanyCode,
                plant = request.PlantCode,
                supplierId = request.SupplierId,
                supplierName = request.SupplierName,
                buyerDocumentNumber = request.BuyerDocumentNumber,
                shipTo = request.ShipTo,
                outletCode = request.OutletCode,
                outletName = request.OutletName,
                currency = request.Currency,
                deliveryInstruction = request.DeliveryInstruction,
                requiredDate = request.RequiredDate,
                lines = request.Lines.Select(line => new
                {
                    materialCode = line.MaterialCode,
                    materialName = line.MaterialName,
                    sku = line.Sku,
                    quantity = line.Quantity,
                    unitOfMeasure = line.UnitOfMeasure,
                    unitPrice = line.UnitPrice,
                    currency = line.Currency,
                    storageLocation = line.StorageLocation
                })
            });
        }

        // Tokens of a configured body. {{wishlistId}} is kept as another name of the weekly bucket id,
        // so bodies saved before the weekly bucket keep working.
        private static string ApplyConfiguredBody(string template, BuyerPurchaseDocumentRequest request)
        {
            string entries = JsonSerializer.Serialize(request.Lines.Select(line => new
            {
                qty = line.Quantity,
                sku = string.IsNullOrWhiteSpace(line.Sku) ? line.MaterialCode : line.Sku,
                uom = line.UnitOfMeasure,
                unitPrice = line.UnitPrice,
                materialCode = line.MaterialCode,
                storageLocation = line.StorageLocation
            }));
            return template
                .Replace("{{wishlistId}}", request.WeeklyBucketId.ToString())
                .Replace("{{weeklyBucketId}}", request.WeeklyBucketId.ToString())
                .Replace("{{bucketCode}}", request.BucketCode)
                .Replace("{{companyCode}}", request.CompanyCode)
                .Replace("{{plant}}", request.PlantCode)
                .Replace("{{supplierId}}", request.SupplierId.ToString())
                .Replace("{{supplierName}}", request.SupplierName ?? string.Empty)
                .Replace("{{buyerDocumentNumber}}", request.BuyerDocumentNumber ?? string.Empty)
                .Replace("{{shipTo}}", request.ShipTo ?? string.Empty)
                .Replace("{{orderDate}}", (request.RequiredDate ?? DateTime.UtcNow).ToString("dd/MM/yyyy"))
                .Replace("{{currency}}", request.Currency ?? string.Empty)
                .Replace("{{deliveryInstruction}}", request.DeliveryInstruction ?? string.Empty)
                .Replace("{{entries}}", entries);
        }
    }
}
