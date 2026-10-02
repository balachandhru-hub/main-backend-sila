using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using SharedKernel.Security;
using Supplier.Application.Contracts;
using Supplier.Domain.Common;

namespace Supplier.Infrastructure.ApiClients
{
    public class OperationsApiClient : IOperationsApiClient
    {
        private const string RUN_PATH = "api/v1/operations/internal/integrations/run";
        private const string PRODUCT_STOCK_PROCESS = "GET_CATALOG_STOCK";

        // A buyer waiting on a refresh is not held up by a slow supplier API for long.
        private static readonly TimeSpan RefreshTimeout = TimeSpan.FromSeconds(20);

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public OperationsApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RefreshProductStock(
            List<Guid> supplierOrganizationIds,
            CancellationToken cancellationToken = default)
        {
            string? operationsUrl = _configuration[Common.OPERATIONS_SERVICE_BASE_URL];
            if (supplierOrganizationIds.Count == 0 || string.IsNullOrWhiteSpace(operationsUrl))
            {
                return;
            }

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RefreshTimeout);
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{operationsUrl.TrimEnd('/')}/{RUN_PATH}");
                request.Content = JsonContent.Create(new
                {
                    organizationIds = supplierOrganizationIds,
                    processType = PRODUCT_STOCK_PROCESS
                });

                // A service-to-service call: it carries the internal key instead of a user's token.
                request.Headers.Add(InternalServiceKey.HEADER, InternalServiceKey.Value(_configuration));

                using HttpResponseMessage response = await _httpClient.SendAsync(request, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Product stock could not be refreshed. HttpStatus: {(int)response.StatusCode}, Suppliers: {supplierOrganizationIds.Count}");
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                // The stock already stored is used; the next refresh tries again.
                _logger.LogError($"Product stock could not be refreshed. Suppliers: {supplierOrganizationIds.Count}, Error: {exception.Message}");
            }
        }
    }
}
