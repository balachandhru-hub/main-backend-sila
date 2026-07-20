using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Identity.Application.Contracts;
using Identity.Domain.Common;
using Identity.Domain.Dto;

namespace Identity.Infrastructure.ApiClients
{
    public class SupplierApiClient : ISupplierApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SupplierApiClient(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task UpdateStatus(
            UpdateOrganizationStatusDto supplier,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var supplierUrl = _configuration[Common.SUPPLIER_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"{supplierUrl}/api/v1/supplier/internal/status");

            request.Content = JsonContent.Create(new
            {
                supplier
            });

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
                    "Unable to update supplier status.",
                    error);
            }
        }
    }
}