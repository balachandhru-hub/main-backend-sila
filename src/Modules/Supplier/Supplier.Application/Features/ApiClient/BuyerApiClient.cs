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
            IConfiguration configuration,IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor=httpContextAccessor;
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
    }
}