using System.Net.Http.Json;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Microsoft.AspNetCore.Http;

namespace Buyer.Infrastructure.ApiClients
{
    public class SupplierApiClient : ISupplierApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
          private readonly ILoggerManager _logger;

private readonly IHttpContextAccessor _httpContextAccessor;
        public SupplierApiClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILoggerManager logger,
             IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

       public async Task CreateSupplierRFQ(
    CreateSupplierRFQRequestDto rfq,
    CancellationToken cancellationToken = default)
{
    _logger.LogInfo($"Creating Supplier RFQ. BuyerRFQId : {rfq.BuyerRFQId}");

    var supplierUrl = _configuration[Common.SUPPLIER_SERVICE_BASE_URL];

    var request = new HttpRequestMessage(
        HttpMethod.Post,
        $"{supplierUrl}/api/v1/supplier/internal-rfq");

    request.Content = JsonContent.Create(rfq);

    // Get access token from current request cookie
    var accessToken = _httpContextAccessor.HttpContext?
        .Request
        .Cookies[Common.ACCESS_TOKEN];

    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        _logger.LogInfo("Adding access token to request headers.");

        request.Headers.Add(
            "Cookie",
            $"{Common.ACCESS_TOKEN}={accessToken}");
    }

    var response = await _httpClient.SendAsync(request, cancellationToken);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogError(
            $"Failed to create Supplier RFQ. Status Code: {response.StatusCode}");

        var error = await response.Content.ReadAsStringAsync();

        throw new BadRequestCustomException(
            "Unable to create Supplier RFQ.",
            error);
    }

    _logger.LogInfo(
        $"Supplier RFQ created successfully. BuyerRFQId : {rfq.BuyerRFQId}");
}
}
}