using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Supplier.Application.Contracts;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;

namespace Supplier.Infrastructure.ApiClients
{
    public class BuyerApiClient : IBuyerApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BuyerApiClient(
            HttpClient httpClient,
            IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Guid>> GetVerifiedSuppliers(
            GetVerifiedSupplierRequestDto requestDto,

            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{buyerUrl}/api/v1/buyer/verified-suppliers");

            request.Content = JsonContent.Create(requestDto);
            var accessToken = _httpContextAccessor.HttpContext?
       .Request.Cookies[Common.ACCESS_TOKEN];


            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add("Cookie", $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch verified suppliers.",
                    error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<List<Guid>>(cancellationToken: cancellationToken);

            return result ?? new List<Guid>();
        }
        public async Task<GetRFQAttachmentsDto> GetRFQAttachments(
    Guid rfqId,
    CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{buyerUrl}/api/v1/buyer/rfq-attachments?rfqId={rfqId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add("Cookie", $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch RFQ attachments.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<GetRFQAttachmentsDto>(
                cancellationToken: cancellationToken);

            return result ?? new GetRFQAttachmentsDto();
        }

        public async Task<SupplierVerificationRequestDetailDto> GetSupplierVerificationRequestDetail(
            Guid requestId,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{buyerUrl}/api/v1/buyer/supplier-verification-request-detail?requestId={requestId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add("Cookie", $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Supplier Verification Request.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<SupplierVerificationRequestDetailDto>(
                cancellationToken: cancellationToken);

            return result ?? new SupplierVerificationRequestDetailDto();
        }

        public async Task UpdateVerificationRequestStatus(
            Guid verificationRequestId,
            string status,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{buyerUrl}/api/v1/buyer/supplier-verification-request-status");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add("Cookie", $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            request.Content = JsonContent.Create(new
            {
                VerificationRequestId = verificationRequestId,
                Status = status
            });

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to update verification request status.",
                    error);
            }
        }

        public async Task<List<RFQQuestionResponseDto>> GetRFQQuestions(
            Guid rfqId,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{buyerUrl}/api/v1/buyer/internal-rfq-questions?rfqId={rfqId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add("Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch RFQ questions.",
                    error);
            }

            var result = await response.Content.ReadFromJsonAsync<List<RFQQuestionResponseDto>>(
                cancellationToken: cancellationToken);

            return result ?? new List<RFQQuestionResponseDto>();
        }
        public async Task<CostCenterDto> GetCostCenterById(
            Guid costCenterId,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{buyerUrl}/api/v1/buyer/cost-center/{costCenterId}");

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch cost center.",
                    error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<CostCenterDto>(
                    cancellationToken: cancellationToken);

            return result ?? new CostCenterDto();
        }
        public async Task StoreQuotationAuditAsync(
  QuotationAuditDto audit,
  CancellationToken cancellationToken)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            if (string.IsNullOrWhiteSpace(buyerUrl))
            {
                throw new BadRequestCustomException(
                    "Buyer service URL is not configured.",
                    "Please configure the Buyer service base URL.");
            }

            var url =
                $"{buyerUrl.TrimEnd('/')}/api/v1/buyer/quotation-audit";

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                url);

            var accessToken = _httpContextAccessor.HttpContext?
                .Request.Cookies[Common.ACCESS_TOKEN];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            request.Content = JsonContent.Create(audit);

            var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                throw new BadRequestCustomException(
                    "Failed to store quotation audit in Buyer service.",
                    $"StatusCode: {response.StatusCode}, Response: {responseContent}");
            }
        }
    }
}