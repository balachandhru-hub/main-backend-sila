using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using Identity.Application.Contracts;
using Identity.Domain.Common;

namespace Identity.Infrastructure.ApiClients
{
    public class SupplierIdApiClient : ISupplierIdApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public SupplierIdApiClient(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<Guid> GetSupplierId(
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var supplierUrl = _configuration[Common.SUPPLIER_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{supplierUrl}/api/v1/supplier/id");

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
                    "Unable to fetch Supplier Id.",
                    error);
            }

            return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken: cancellationToken);
        }
    }
}