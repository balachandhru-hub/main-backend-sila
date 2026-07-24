using System.Net.Http.Json;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;

namespace Buyer.Infrastructure.ApiClients
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

        public async Task CreateSupplierRFQ(
             CreateSupplierRFQRequestDto rfq,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{supplierUrl}/api/v1/supplier/internal-rfq");

            request.Content = JsonContent.Create(rfq);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Add(
                    "Cookie",
                    $"{Common.ACCESS_TOKEN}={accessToken}");
            }

            var response =
                await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new BadRequestCustomException(
                    "Unable to create Supplier RFQ.",
                    error);
            }
        }
    }
}