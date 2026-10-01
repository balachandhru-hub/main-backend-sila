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

        public async Task<ExternalSupplierSessionDto?> ValidateExternalSessionToken(
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
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ExternalSupplierSessionDto>(
                cancellationToken: cancellationToken);
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

        public async Task<List<SupplierNameDto>> GetSupplierNamesByIds(
            List<Guid> supplierIds,
            CancellationToken cancellationToken = default)
        {
            if (supplierIds == null || !supplierIds.Any())
            {
                _logger.LogInfo("No Supplier IDs provided. Returning empty list.");
               throw new NotFoundCustomException(
                    "No Supplier IDs provided.",
                    "The list of Supplier IDs is empty or null.");
            }

            _logger.LogInfo($"Fetching Supplier Names. Count: {supplierIds.Count}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var queryString = string.Join(
                "&",
                supplierIds.Select(id => $"supplierIds={id}"));

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/internal-names?{queryString}");

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
                    $"Failed to fetch Supplier Names. Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Supplier Names.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<List<SupplierNameDto>>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"Supplier Names fetched successfully. Count: {result?.Count ?? 0}");

            return result ?? new List<SupplierNameDto>();
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

        public async Task SaveSupplierRFQAward(
            SupplierRFQAwardRequestDto requestDto,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo(
                $"Pushing RFQ award to Supplier service. " +
                $"BuyerRFQId: {requestDto.BuyerRFQId}, " +
                $"Items: {requestDto.Items.Count}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{supplierUrl}/api/v1/supplier/internal/rfq-award");

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
                    $"Failed to push RFQ award to Supplier service. " +
                    $"BuyerRFQId: {requestDto.BuyerRFQId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to push RFQ award to Supplier service.",
                    error);
            }

            _logger.LogInfo(
                $"RFQ award pushed to Supplier service successfully. " +
                $"BuyerRFQId: {requestDto.BuyerRFQId}");
        }

        public async Task ResetSupplierRFQAward(
            Guid buyerRFQId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo(
                $"Resetting RFQ award on Supplier service. BuyerRFQId: {buyerRFQId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{supplierUrl}/api/v1/supplier/internal/rfq-award/reset");

            request.Content = JsonContent.Create(new { BuyerRFQId = buyerRFQId });

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
                    $"Failed to reset RFQ award on Supplier service. " +
                    $"BuyerRFQId: {buyerRFQId}, Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to reset RFQ award on Supplier service.",
                    error);
            }

            _logger.LogInfo(
                $"RFQ award reset on Supplier service successfully. " +
                $"BuyerRFQId: {buyerRFQId}");
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

        public async Task NotifyNewMessage(
            MessageResponseDto message,
            CancellationToken cancellationToken = default)
        {
            Guid threadId = message.ThreadId;

            _logger.LogInfo($"Relaying new message notification. ThreadId: {threadId}, RFQId: {message.RFQId}, SupplierId: {message.SupplierId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{supplierUrl}/api/v1/supplier/message/internal/notify");

            request.Content = JsonContent.Create(message);

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to relay new message notification. ThreadId: {threadId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to relay new message notification.",
                    error);
            }

            _logger.LogInfo($"New message notification relayed successfully. ThreadId: {threadId}");
        }

        public async Task<List<RFQTermsConditionDto>> GetRFQTermsCondition(
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching Terms and Condition. RFQId: {rfqId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/internal/rfq-terms-condition?rfqId={rfqId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch Terms and Condition. RFQId: {rfqId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Terms and Condition.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<List<RFQTermsConditionDto>>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"Terms and Condition fetched successfully. RFQId: {rfqId}");

            return result ?? new List<RFQTermsConditionDto>();
        }

        public async Task<List<RFQESignDto>> GetRFQESign(
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching E-Sign. RFQId: {rfqId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/internal/rfq-esign?rfqId={rfqId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch E-Sign. RFQId: {rfqId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch E-Sign.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<List<RFQESignDto>>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"E-Sign fetched successfully. RFQId: {rfqId}");

            return result ?? new List<RFQESignDto>();
        }

        public async Task<List<BuyerTermsAndConditionStatusDto>> GetBuyerTermsConditionStatus(
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching Buyer Terms and Condition status. RFQId: {rfqId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/internal/buyer-terms-condition-status?rfqId={rfqId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch Buyer Terms and Condition status. RFQId: {rfqId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Buyer Terms and Condition status.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<List<BuyerTermsAndConditionStatusDto>>(
                cancellationToken: cancellationToken);

            _logger.LogInfo($"Buyer Terms and Condition status fetched successfully. RFQId: {rfqId}");

            return result ?? new List<BuyerTermsAndConditionStatusDto>();
        }

        public async Task InviteSupplierForContract(
            Guid rfqId,
            Guid supplierId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo(
                $"Inviting supplier for contract. RFQId: {rfqId}, SupplierId: {supplierId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{supplierUrl}/api/v1/supplier/rfq/invite-for-contract");

            var requestDto = new
            {
                RFQId = rfqId,
                SupplierId = supplierId
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

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to invite supplier for contract. RFQId: {rfqId}, SupplierId: {supplierId}, " +
                    $"Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to invite supplier for contract.",
                    error);
            }

            _logger.LogInfo(
                $"Supplier invited for contract successfully. RFQId: {rfqId}, SupplierId: {supplierId}");
        }

        public async Task<BuyerCatalogItemDto?> GetBuyerCatalogById(
            Guid catalogId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInfo($"Fetching product catalog item. CatalogId: {catalogId}");

            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/buyer-catalog/{catalogId}");

            // Get access token from current request cookie
            var accessToken = _httpContextAccessor.HttpContext?
                .Request
                .Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogError($"Product catalog item not found. CatalogId: {catalogId}");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    $"Failed to fetch product catalog item. CatalogId: {catalogId}, Status Code: {response.StatusCode}");

                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch the product catalog item.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<List<BuyerCatalogItemDto>>(
                cancellationToken: cancellationToken);

            return result?.FirstOrDefault();
        }
    }
}
