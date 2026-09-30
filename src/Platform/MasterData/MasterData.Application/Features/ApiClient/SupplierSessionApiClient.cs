using MasterData.Application.Contracts;
using MasterData.Domain.Common;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.ApiClient
{
    public class SupplierSessionApiClient : ISupplierSessionApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SupplierSessionApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ValidateExternalSessionToken(
            string sessionToken,
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/internal-session-token/validate" +
                $"?sessionToken={Uri.EscapeDataString(sessionToken)}&rfqId={rfqId}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"External session token validation failed. RFQId: {rfqId}, Status Code: {response.StatusCode}");
            }

            return response.IsSuccessStatusCode;
        }
    }
}
