using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Microsoft.AspNetCore.Http;

namespace Buyer.Infrastructure.ApiClients
{
    public class IdentityApiClient : IIdentityApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public IdentityApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task UpdateOrganization(
            UpdateBuyerBusinessProfileDto organization,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var identityUrl = _configuration[Common.IDENTITY_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{identityUrl}/api/v1/identity/update-organization");

            request.Content = JsonContent.Create(new
            {
                organization
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
                    "Unable to update organization.",
                    error);
            }
        }
        public async Task<List<ModelDto>> GetOrganizationModels(
    Guid? organizationId = null,
    CancellationToken cancellationToken = default)
        {
            var identityUrl = _configuration[Common.IDENTITY_SERVICE_BASE_URL];

            var url = $"{identityUrl}/api/v1/identity/get-organization-model";

            if (organizationId.HasValue)
            {
                url += $"?organizationId={organizationId.Value}";
            }

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                url);

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
                    "Unable to fetch organization models.",
                    error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<List<ModelDto>>(
                    cancellationToken: cancellationToken);

            return result ?? new List<ModelDto>();
        }
    }
}