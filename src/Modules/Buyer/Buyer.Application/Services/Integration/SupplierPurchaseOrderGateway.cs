using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Buyer.Domain.Common;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services.Integration
{
    public interface ISupplierPurchaseOrderGateway
    {
        Task<ExternalCallResult> CreateSupplierPurchaseOrderAsync(
            SupplierPurchaseOrderRequest request,
            CancellationToken cancellationToken);
    }

    public class SupplierPurchaseOrderGateway : ISupplierPurchaseOrderGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SupplierPurchaseOrderGateway(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ExternalCallResult> CreateSupplierPurchaseOrderAsync(
            SupplierPurchaseOrderRequest request,
            CancellationToken cancellationToken)
        {
            string? supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];
            string? apiKey = _configuration[Common.INTERNAL_API_KEY];
            if (string.IsNullOrWhiteSpace(supplierUrl) || string.IsNullOrWhiteSpace(apiKey))
            {
                return new ExternalCallResult
                {
                    Succeeded = false,
                    StatusCode = 500,
                    ErrorMessage = "Supplier service URL or internal API key is not configured."
                };
            }

            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(90);
            using HttpRequestMessage message = new HttpRequestMessage(
                HttpMethod.Post,
                supplierUrl.TrimEnd('/') + Common.SUPPLIER_PURCHASE_ORDER_PATH);
            message.Headers.TryAddWithoutValidation("X-Internal-Api-Key", apiKey);
            message.Headers.TryAddWithoutValidation("X-Correlation-Id", request.CorrelationId);
            message.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                string? documentNumber = ExternalDocumentNumberReader.TryRead(body);
                _logger.LogInfo(
                    $"Supplier purchase order call finished. WishlistId={request.WishlistId} BuyerOrganizationId={request.BuyerOrganizationId} " +
                    $"SupplierOrganizationId={request.SupplierOrganizationId} IntegrationType={Common.INTEGRATION_SUPPLIER_ERP} " +
                    $"CorrelationId={request.CorrelationId} StatusCode={(int)response.StatusCode}");

                return new ExternalCallResult
                {
                    Succeeded = response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(documentNumber),
                    StatusCode = (int)response.StatusCode,
                    DocumentNumber = documentNumber,
                    ResponseBody = body.Length <= 4000 ? body : body.Substring(0, 4000),
                    OutcomeUnknown = !response.IsSuccessStatusCode && (int)response.StatusCode >= 500,
                    ErrorMessage = response.IsSuccessStatusCode
                        ? (string.IsNullOrWhiteSpace(documentNumber) ? "Supplier service did not return a document number." : null)
                        : $"Supplier service returned HTTP {(int)response.StatusCode}."
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(
                    $"Supplier purchase order call did not complete. WishlistId={request.WishlistId} CorrelationId={request.CorrelationId} Error={ex.Message}");
                return new ExternalCallResult
                {
                    Succeeded = false,
                    StatusCode = 0,
                    OutcomeUnknown = true,
                    ErrorMessage = "Supplier service call did not complete. Automatic retry is skipped because the supplier order may already exist."
                };
            }
        }
    }
}
