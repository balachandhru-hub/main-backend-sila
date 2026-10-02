using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Operations.Domain.Dtos;
using SharedKernel.LoggerServices;
using SharedKernel.Security;

namespace Operations.Application.Services.Integration
{
    public interface ISupplierCatalogSyncClient
    {
        /// <summary>
        /// Writes products (or product stock) read from a supplier's API into that supplier's catalog,
        /// which the Supplier service owns.
        /// </summary>
        Task<CatalogSyncResultDto> SyncAsync(CatalogSyncRequestDto request, CancellationToken cancellationToken);
    }

    public class SupplierCatalogSyncClient : ISupplierCatalogSyncClient
    {
        public const string SYNC_PATH = "api/v1/supplier/internal/catalog-sync";
        private const string SUPPLIER_URL_CONFIG = "InterCallService:SupplierUrl";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SupplierCatalogSyncClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILoggerManager logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CatalogSyncResultDto> SyncAsync(CatalogSyncRequestDto request, CancellationToken cancellationToken)
        {
            string? supplierUrl = _configuration[SUPPLIER_URL_CONFIG];
            if (string.IsNullOrWhiteSpace(supplierUrl))
            {
                throw new IntegrationException("SUPPLIER_SERVICE_NOT_CONFIGURED", $"'{SUPPLIER_URL_CONFIG}' is not configured.");
            }

            using HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, $"{supplierUrl.TrimEnd('/')}/{SYNC_PATH}");
            message.Content = JsonContent.Create(request);

            // A service-to-service call: it carries the internal key instead of a user's token.
            message.Headers.Add(InternalServiceKey.HEADER, InternalServiceKey.Value(_configuration));

            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Supplier catalog could not be updated. OrganizationId: {request.OrganizationId}, HttpStatus: {(int)response.StatusCode}");
                    throw new IntegrationException("CATALOG_UPDATE_FAILED", "The supplier catalog could not be updated.", 424);
                }

                return await response.Content.ReadFromJsonAsync<CatalogSyncResultDto>(cancellationToken: cancellationToken) ?? new CatalogSyncResultDto();
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError($"Supplier service could not be reached. OrganizationId: {request.OrganizationId}, Error: {exception.Message}");
                throw new IntegrationException("CATALOG_UPDATE_FAILED", "The supplier catalog could not be updated.", 424);
            }
        }
    }
}
