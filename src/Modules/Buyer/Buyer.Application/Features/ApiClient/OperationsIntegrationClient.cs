using System.Net.Http.Json;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using SharedKernel.Security;

namespace Buyer.Infrastructure.ApiClients
{
    public class OperationsIntegrationClient : IOperationsIntegrationClient
    {
        private const string RESOLVE_PATH = "api/v1/operations/internal/integrations/resolve";
        private const string SEND_PATH = "api/v1/operations/internal/integrations/send";
        private const string STOCK_PATH = "api/v1/operations/internal/integrations/stock";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public OperationsIntegrationClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IntegrationApiDto> ResolveAsync(
            Guid organizationId,
            string processType,
            string? entityCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpRequestMessage request = NewRequest(RESOLVE_PATH, new { organizationId, processType, entityCode });
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"API configuration could not be read. OrganizationId: {organizationId}, ProcessType: {processType}, HttpStatus: {(int)response.StatusCode}");
                    return new IntegrationApiDto { Configured = false };
                }

                return await response.Content.ReadFromJsonAsync<IntegrationApiDto>(cancellationToken: cancellationToken)
                    ?? new IntegrationApiDto { Configured = false };
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError($"Integration service could not be reached. OrganizationId: {organizationId}, ProcessType: {processType}, Error: {exception.Message}");
                return new IntegrationApiDto { Configured = false };
            }
        }

        public async Task<IntegrationSendResultDto> SendAsync(
            Guid organizationId,
            Guid configurationId,
            string body,
            Dictionary<string, string> headers,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpRequestMessage request = NewRequest(SEND_PATH, new { organizationId, configurationId, body, headers });
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    // The integration service refused the request itself, so nothing was sent to the API.
                    _logger.LogError($"Document was not sent. ConfigurationId: {configurationId}, HttpStatus: {(int)response.StatusCode}");
                    return new IntegrationSendResultDto
                    {
                        Answered = false,
                        StatusCode = (int)response.StatusCode,
                        ErrorCode = "INTEGRATION_REFUSED",
                        ErrorMessage = "The API is not active. Activate it under Integrations."
                    };
                }

                return await response.Content.ReadFromJsonAsync<IntegrationSendResultDto>(cancellationToken: cancellationToken)
                    ?? new IntegrationSendResultDto { Answered = false, OutcomeUnknown = true, ErrorMessage = "The integration service gave no result." };
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                // The integration service may already have sent the document when the connection to it broke.
                _logger.LogError($"Integration service call did not complete. ConfigurationId: {configurationId}, Error: {exception.Message}");
                return new IntegrationSendResultDto
                {
                    Answered = false,
                    OutcomeUnknown = true,
                    ErrorCode = "INTEGRATION_UNAVAILABLE",
                    ErrorMessage = "The integration service did not answer."
                };
            }
        }

        public async Task<StockInHandResponseDto> GetStockInHandAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpRequestMessage request = NewRequest(STOCK_PATH, new { organizationId });
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Stock in hand could not be read. OrganizationId: {organizationId}, HttpStatus: {(int)response.StatusCode}");
                    return new StockInHandResponseDto();
                }

                return await response.Content.ReadFromJsonAsync<StockInHandResponseDto>(cancellationToken: cancellationToken)
                    ?? new StockInHandResponseDto();
            }
            catch (HttpRequestException exception)
            {
                // Stock in hand is then not known; the weekly bucket shows it as such.
                _logger.LogError($"Integration service could not be reached. OrganizationId: {organizationId}, Error: {exception.Message}");
                return new StockInHandResponseDto();
            }
        }

        private HttpRequestMessage NewRequest(string path, object body)
        {
            string operationsUrl = _configuration[Common.OPERATIONS_SERVICE_BASE_URL]
                ?? throw new InvalidOperationException($"'{Common.OPERATIONS_SERVICE_BASE_URL}' is not configured.");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{operationsUrl.TrimEnd('/')}/{path}");
            request.Content = JsonContent.Create(body);

            // A service-to-service call: it carries the internal key instead of a user's token.
            request.Headers.Add(InternalServiceKey.HEADER, InternalServiceKey.Value(_configuration));
            return request;
        }
    }
}
