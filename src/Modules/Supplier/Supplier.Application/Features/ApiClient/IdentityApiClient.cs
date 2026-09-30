using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Supplier.Application.Contracts;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;

namespace Supplier.Infrastructure.ApiClients
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
            UpdateOrganizationRequestDto organization,
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

        public async Task<List<IdentityUserDto>> GetUsersByIds(
            List<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds == null || !userIds.Any())
            {
                return new List<IdentityUserDto>();
            }

            var identityUrl = _configuration[Common.IDENTITY_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{identityUrl}/api/v1/identity/users-by-ids");

            request.Content = JsonContent.Create(userIds);

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
                    "Unable to fetch users.",
                    error);
            }

            var result = await response.Content
                .ReadFromJsonAsync<List<IdentityUserDto>>(
                    cancellationToken: cancellationToken);

            return result ?? new List<IdentityUserDto>();
        }
    }
}