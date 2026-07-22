using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Identity.Application.Contracts;
using Identity.Domain.Common;

namespace Identity.Infrastructure.ApiClients
{
    public class BuyerIdApiClient : IBuyerIdApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public BuyerIdApiClient(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<Guid> GetBuyerId(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{buyerUrl}/api/v1/buyer/id");

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to fetch Buyer Id.",
                    error);
            }

            return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: cancellationToken);
        }
    }
}