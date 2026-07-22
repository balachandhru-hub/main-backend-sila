using System.Net.Http.Json;
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

        public BuyerApiClient(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<Guid>> GetVerifiedSuppliers(
            GetVerifiedSupplierRequestDto requestDto,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{buyerUrl}/api/v1/buyer/get-verified-suppliers");

            request.Content = JsonContent.Create(requestDto);

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
    }
}