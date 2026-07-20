using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Identity.Application.Contracts;
using Identity.Domain.Common;
using Identity.Domain.Dto;

namespace Identity.Infrastructure.ApiClients
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

        public async Task UpdateStatus(
            UpdateOrganizationStatusDto buyer,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var buyerUrl = _configuration[Common.BUYER_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{buyerUrl}/api/v1/buyer/internal/status");

            request.Content = JsonContent.Create(new
            {
                buyer
            });

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add("Cookie", $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to update buyer status.",
                    error);
            }
        }
    }
}