using System.Net.Http.Json;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Microsoft.AspNetCore.Http;

namespace Buyer.Infrastructure.ApiClients
{
    public class SupplierApiClient : ISupplierApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;
        public SupplierApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILoggerManager logger,
             IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
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

            return response.IsSuccessStatusCode;
        }

        public async Task CreateSupplierRFQ(
     CreateSupplierRFQRequestDto rfq,
     CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Creating Supplier RFQ. BuyerRFQId : {rfq.BuyerRFQId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{supplierUrl}/api/v1/supplier/internal-rfq");

            request.Content = JsonContent.Create(rfq);

            // Get access token from current request cookie
            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInfo("Adding access token to request headers.");

                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to create Supplier RFQ. Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to create Supplier RFQ.",
                    error);
            }

            _logger.LogInfo(
                $"Supplier RFQ created successfully. BuyerRFQId : {rfq.BuyerRFQId}");
        }

        public async Task<GetAllSupplierQuotationDto> GetSupplierQuotation(
            Guid RFQId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching Supplier Quotation. BuyerRFQId: {RFQId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/quotation-rfq-by-id?rfqId={RFQId}");

            // Get access token from current request cookie
            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInfo("Adding access token to request headers.");

                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch Supplier Quotation. Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Supplier Quotation.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<GetAllSupplierQuotationDto>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"Supplier Quotation fetched successfully. BuyerRFQId: {RFQId}");

            return result ?? new GetAllSupplierQuotationDto();
        }

        public async Task<SupplierProfileDto> GetSupplierById(
    Guid supplierId,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching Supplier Details. SupplierId: {supplierId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/{supplierId}");

            // Get access token from current request cookie
            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInfo("Adding access token to request headers.");

                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch Supplier Details. Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Supplier Details.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<SupplierProfileDto>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"Supplier Details fetched successfully. SupplierId: {supplierId}");

            return result ?? new SupplierProfileDto();
        }
        public async Task<GetQuestionsAnswersForSupplierDto> GetQuestionsAnswersForSupplier(
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/questions-answers?requestId={requestId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                requestMessage.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(
                requestMessage,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch supplier verification answers.",
                    error);
            }

            var result =
                await response.Content.ReadFromJsonAsync<GetQuestionsAnswersForSupplierDto>(
                    cancellationToken: cancellationToken);

            return result ?? new GetQuestionsAnswersForSupplierDto();
        }
        public async Task<SupplierRFQAnswerDto> GetSupplierRFQAnswers(
    Guid buyerRFQId,
    CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching Supplier RFQ Answers. BuyerRFQId: {buyerRFQId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];
            _logger.LogInfo($"Supplier Service Base URL: {supplierUrl}");
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/internal-rfq-answer/{buyerRFQId}");
            _logger.LogInfo($"Request URL: {request.RequestUri}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInfo("Adding access token to request headers.");

                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch Supplier RFQ Answers. Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Error response: {error}");
                throw new BadRequestCustomException(
                    "Unable to fetch Supplier RFQ Answers.",
                    error);
            }
            _logger.LogInfo($"Supplier RFQ Answers fetched successfully. BuyerRFQId: {buyerRFQId}");
            var supplierAnswers =
      await response.Content.ReadFromJsonAsync<SupplierRFQAnswerDto>(
          cancellationToken: cancellationToken);

            if (supplierAnswers == null)
            {
                _logger.LogInfo(
                    $"No Supplier RFQ Answers found for BuyerRFQId: {buyerRFQId}");

                return new SupplierRFQAnswerDto();
            }

            _logger.LogInfo(
                $"Supplier RFQ Answers found for BuyerRFQId: {buyerRFQId}. " +
                $"Total suppliers: {supplierAnswers.Suppliers.Count}");

            return supplierAnswers;
        }

        public async Task<Guid> GetSupplierId(
    CancellationToken cancellationToken = default)
        {
            var supplierUrl =
                _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var requestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/id");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                requestMessage.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(
                requestMessage,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Supplier Id.",
                    error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<Guid>(
                    cancellationToken: cancellationToken);

            return result;
        }
        public async Task UpdateSupplierRFQStatus(Guid rfqId,string status,CancellationToken cancellationToken = default)
        {
            _logger.LogInfo(
                $"Updating Supplier RFQ status. BuyerRFQId: {rfqId}, Status: {status}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{supplierUrl}/api/v1/supplier/rfq-status");

            var requestDto = new
            {
                RFQId = rfqId,
                Status = status
            };

            request.Content = JsonContent.Create(requestDto);

            // Get access token from current request cookie
            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInfo("Adding access token to request headers.");

                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to update Supplier RFQ status. " +
                    $"RFQId: {rfqId}, Status: {status}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to update Supplier RFQ status.",
                    error);
            }

            _logger.LogInfo(
                $"Supplier RFQ status updated successfully. " +
                $"RFQId: {rfqId}, Status: {status}");
        }

        public async Task<BidCompareResponseDto> GetBidCompare(
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching Bid Compare. RFQId: {rfqId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/bid-compare?rfqId={rfqId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogInfo("Adding access token to request headers.");

                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch Bid Compare. RFQId: {rfqId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Bid Compare.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<BidCompareResponseDto>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"Bid Compare fetched successfully. RFQId: {rfqId}");

            return result ?? new BidCompareResponseDto();
        }
    }
}
