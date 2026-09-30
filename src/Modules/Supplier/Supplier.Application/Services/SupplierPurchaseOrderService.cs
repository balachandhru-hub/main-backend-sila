using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Services
{
    public interface ISupplierPurchaseOrderService
    {
        Task<SupplierPurchaseOrderResponseDto> CreateAsync(SupplierPurchaseOrderRequestDto request, CancellationToken cancellationToken);
        Task<SupplierErpResponseDto?> GetConfigurationAsync(Guid supplierOrganizationId, CancellationToken cancellationToken);
        Task<Guid> SaveConfigurationAsync(Guid supplierOrganizationId, SupplierErpWriteDto request, CancellationToken cancellationToken);
    }

    public class SupplierPurchaseOrderService : ISupplierPurchaseOrderService
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerManager _logger;

        public SupplierPurchaseOrderService(
            IRepositoryWrapper repository,
            IHttpClientFactory httpClientFactory,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<SupplierPurchaseOrderResponseDto> CreateAsync(
            SupplierPurchaseOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request.WishlistId == Guid.Empty || request.SupplierOrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                throw new BadRequestCustomException("Purchase order request is incomplete.", "Wishlist, supplier, and idempotency key are required.");
            }

            if (string.IsNullOrWhiteSpace(request.BuyerDocumentNumber))
            {
                throw new BadRequestCustomException("Buyer document number is required.", "Create the supplier purchase order only after the buyer ERP document exists.");
            }

            SupplierPurchaseDocument? existing = await _repository.SupplierErp.GetDocumentByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existing != null && existing.Status == Common.INTEGRATION_SUCCEEDED && !string.IsNullOrWhiteSpace(existing.SupplierDocumentNumber))
            {
                return new SupplierPurchaseOrderResponseDto
                {
                    DocumentNumber = existing.SupplierDocumentNumber,
                    Status = Common.INTEGRATION_SUCCEEDED
                };
            }

            SupplierErpIntegrationConfiguration? configuration = await _repository.SupplierErp.GetByOrganizationAsync(request.SupplierOrganizationId, cancellationToken);
            if (configuration == null || !configuration.IsActive)
            {
                throw new BadRequestCustomException(
                    "Supplier ERP is not configured.",
                    "Save an active supplier ERP configuration before creating a purchase order.");
            }

            SupplierPurchaseDocument document = existing ?? new SupplierPurchaseDocument
            {
                Id = Guid.NewGuid(),
                WishlistId = request.WishlistId,
                BuyerOrganizationId = request.BuyerOrganizationId,
                SupplierOrganizationId = request.SupplierOrganizationId,
                IdempotencyKey = request.IdempotencyKey,
                Status = Common.INTEGRATION_FAILED
            };
            if (existing == null)
            {
                _repository.SupplierErp.AddDocument(document);
            }

            if (string.IsNullOrWhiteSpace(document.ResolvedBaseUrl))
            {
                document.ConfigurationId = configuration.Id;
                document.ConfigurationVersion = configuration.Version;
                document.ResolvedBaseUrl = configuration.BaseUrl;
                document.ResolvedOrderPath = configuration.OrderPath;
            }

            document.BuyerDocumentType = request.BuyerDocumentType;
            document.BuyerDocumentNumber = request.BuyerDocumentNumber;
            document.SupplierDocumentType = Common.ERP_DOCUMENT_PO;
            document.CorrelationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId;
            document.LastAttemptOn = DateTime.UtcNow;
            document.RetryCount += 1;
            await _repository.SaveAsync();

            ExternalResult result = string.Equals(configuration.ErpType, Common.ERP_TYPE_ANE_DCI, StringComparison.OrdinalIgnoreCase)
                ? await SendAneOrderAsync(configuration, document, request, cancellationToken)
                : await SendGenericOrderAsync(configuration, document, request, cancellationToken);

            document.ResponseBody = result.Body;
            document.OutcomeUnknown = result.OutcomeUnknown;
            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.DocumentNumber))
            {
                document.Status = Common.INTEGRATION_SUCCEEDED;
                document.SupplierDocumentNumber = result.DocumentNumber;
                document.ErrorMessage = result.ErrorMessage;
                await _repository.SaveAsync();
                _logger.LogInfo(
                    $"Supplier ERP order stored. WishlistId={request.WishlistId} SupplierOrganizationId={request.SupplierOrganizationId} " +
                    $"ConfigurationId={configuration.Id} CorrelationId={document.CorrelationId} StatusCode={result.StatusCode} DurationMs={result.DurationMs} RetryCount={document.RetryCount}");
                return new SupplierPurchaseOrderResponseDto
                {
                    DocumentNumber = result.DocumentNumber,
                    Status = Common.INTEGRATION_SUCCEEDED,
                    ErrorMessage = result.ErrorMessage
                };
            }

            document.Status = result.OutcomeUnknown ? Common.INTEGRATION_UNKNOWN : Common.INTEGRATION_FAILED;
            document.ErrorMessage = result.ErrorMessage;
            await _repository.SaveAsync();
            _logger.LogError(
                $"Supplier ERP order failed. WishlistId={request.WishlistId} SupplierOrganizationId={request.SupplierOrganizationId} " +
                $"ConfigurationId={configuration.Id} CorrelationId={document.CorrelationId} StatusCode={result.StatusCode} RetryCount={document.RetryCount}");
            throw new FailedDependencyCustomException(
                "Supplier ERP did not create a purchase order.",
                result.ErrorMessage ?? "The supplier ERP call failed.");
        }

        public async Task<SupplierErpResponseDto?> GetConfigurationAsync(Guid supplierOrganizationId, CancellationToken cancellationToken)
        {
            SupplierErpIntegrationConfiguration? configuration = await _repository.SupplierErp.GetByOrganizationAsync(supplierOrganizationId, cancellationToken);
            if (configuration == null)
            {
                return null;
            }

            return new SupplierErpResponseDto
            {
                Id = configuration.Id,
                ErpType = configuration.ErpType,
                BaseUrl = configuration.BaseUrl,
                AuthPath = configuration.AuthPath,
                OrderPath = configuration.OrderPath,
                HttpMethod = configuration.HttpMethod,
                AuthType = configuration.AuthType,
                TokenUrl = configuration.TokenUrl,
                Username = configuration.Username,
                HasPassword = !string.IsNullOrWhiteSpace(configuration.Password),
                ClientId = configuration.ClientId,
                HasClientSecret = !string.IsNullOrWhiteSpace(configuration.ClientSecret),
                Scope = configuration.Scope,
                ApiKeyHeader = configuration.ApiKeyHeader,
                HasApiKey = !string.IsNullOrWhiteSpace(configuration.ApiKey),
                HasAccessToken = !string.IsNullOrWhiteSpace(configuration.AccessToken),
                DefaultShipTo = configuration.DefaultShipTo,
                OrderDateFormat = configuration.OrderDateFormat,
                HeadersJson = configuration.HeadersJson,
                TimeoutSeconds = configuration.TimeoutSeconds,
                MaxRetryCount = configuration.MaxRetryCount,
                Version = configuration.Version,
                IsActive = configuration.IsActive
            };
        }

        public async Task<Guid> SaveConfigurationAsync(Guid supplierOrganizationId, SupplierErpWriteDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ErpType) || string.IsNullOrWhiteSpace(request.BaseUrl) || string.IsNullOrWhiteSpace(request.OrderPath) || string.IsNullOrWhiteSpace(request.AuthType))
            {
                throw new BadRequestCustomException("Supplier ERP configuration is incomplete.", "ERP type, base URL, order path, and authentication type are required.");
            }

            if (!request.BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !request.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Base URL is invalid.", "Enter an absolute http or https base URL.");
            }

            SupplierErpIntegrationConfiguration? existing = await _repository.SupplierErp.GetByOrganizationAsync(supplierOrganizationId, cancellationToken);
            if (existing == null)
            {
                existing = new SupplierErpIntegrationConfiguration
                {
                    Id = Guid.NewGuid(),
                    SupplierOrganizationId = supplierOrganizationId,
                    Version = 1
                };
                Apply(existing, request, false);
                _repository.SupplierErp.Create(existing);
            }
            else
            {
                Apply(existing, request, true);
                existing.Version += 1;
            }

            await _repository.SaveAsync();
            return existing.Id;
        }

        private async Task<ExternalResult> SendAneOrderAsync(
            SupplierErpIntegrationConfiguration configuration,
            SupplierPurchaseDocument document,
            SupplierPurchaseOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            string shipTo = !string.IsNullOrWhiteSpace(request.ShipTo) ? request.ShipTo : configuration.DefaultShipTo ?? string.Empty;
            if (string.IsNullOrWhiteSpace(shipTo))
            {
                return ExternalResult.Definite(400, "Ship-to is required by the supplier order API. Set the outlet ship-to or the supplier default ship-to.");
            }

            if (request.Lines == null || request.Lines.Count == 0)
            {
                return ExternalResult.Definite(400, "The supplier order API requires at least one entry.");
            }

            HttpClient client = CreateClient(configuration);
            string? token = await LoginAsync(client, configuration, document, cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return ExternalResult.Definite(401, "Supplier login did not return an access token.");
            }

            string format = string.IsNullOrWhiteSpace(configuration.OrderDateFormat) ? "dd/MM/yyyy" : configuration.OrderDateFormat;
            DateTime orderDate = request.RequiredDate ?? DateTime.UtcNow;
            var payload = new
            {
                shipTo,
                orderDate = orderDate.ToString(format),
                purchaseOrderNo = request.BuyerDocumentNumber,
                deliveryInstruction = request.DeliveryInstruction ?? string.Empty,
                extOrderNo = request.WishlistId.ToString(),
                entries = request.Lines.Select(line => new
                {
                    qty = line.Quantity,
                    sku = line.Sku,
                    uom = line.UnitOfMeasure,
                    unitPrice = line.UnitPrice
                })
            };

            return await PostOrderAsync(client, configuration, document, payload, token, cancellationToken, request.WishlistId.ToString());
        }

        private async Task<ExternalResult> SendGenericOrderAsync(
            SupplierErpIntegrationConfiguration configuration,
            SupplierPurchaseDocument document,
            SupplierPurchaseOrderRequestDto request,
            CancellationToken cancellationToken)
        {
            HttpClient client = CreateClient(configuration);
            string? token = null;
            if (configuration.AuthType == Common.AUTH_OAUTH2_CLIENT_CREDENTIALS || configuration.AuthType == Common.AUTH_DCI_PASSWORD)
            {
                token = await LoginAsync(client, configuration, document, cancellationToken);
                if (string.IsNullOrWhiteSpace(token))
                {
                    return ExternalResult.Definite(401, "Supplier authentication did not return an access token.");
                }
            }

            return await PostOrderAsync(client, configuration, document, request, token, cancellationToken, null);
        }

        private async Task<string?> LoginAsync(
            HttpClient client,
            SupplierErpIntegrationConfiguration configuration,
            SupplierPurchaseDocument document,
            CancellationToken cancellationToken)
        {
            if (configuration.AuthType == Common.AUTH_BEARER)
            {
                return configuration.AccessToken;
            }

            if (configuration.AuthType != Common.AUTH_DCI_PASSWORD && configuration.AuthType != Common.AUTH_OAUTH2_CLIENT_CREDENTIALS)
            {
                return configuration.AccessToken;
            }

            string? loginUrl = configuration.AuthType == Common.AUTH_OAUTH2_CLIENT_CREDENTIALS
                ? configuration.TokenUrl
                : Combine(document.ResolvedBaseUrl ?? configuration.BaseUrl, configuration.AuthPath);
            if (string.IsNullOrWhiteSpace(loginUrl))
            {
                return null;
            }

            HttpContent content = configuration.AuthType == Common.AUTH_OAUTH2_CLIENT_CREDENTIALS
                ? new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = configuration.ClientId ?? string.Empty,
                    ["client_secret"] = configuration.ClientSecret ?? string.Empty,
                    ["scope"] = configuration.Scope ?? string.Empty
                })
                : new StringContent(JsonSerializer.Serialize(new
                {
                    username = configuration.Username,
                    password = configuration.Password
                }), Encoding.UTF8, "application/json");

            using HttpRequestMessage login = new HttpRequestMessage(HttpMethod.Post, loginUrl) { Content = content };
            using HttpResponseMessage response = await client.SendAsync(login, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Supplier login failed. ConfigurationId={configuration.Id} CorrelationId={document.CorrelationId} StatusCode={(int)response.StatusCode}");
                return null;
            }

            return ReadToken(body);
        }

        private async Task<ExternalResult> PostOrderAsync(
            HttpClient client,
            SupplierErpIntegrationConfiguration configuration,
            SupplierPurchaseDocument document,
            object payload,
            string? bearerToken,
            CancellationToken cancellationToken,
            string? fallbackDocumentNumber)
        {
            string endpoint = Combine(document.ResolvedBaseUrl ?? configuration.BaseUrl, document.ResolvedOrderPath ?? configuration.OrderPath);
            using HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(string.IsNullOrWhiteSpace(configuration.HttpMethod) ? "POST" : configuration.HttpMethod), endpoint);
            message.Headers.TryAddWithoutValidation("X-Correlation-Id", document.CorrelationId);
            message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
            else if (configuration.AuthType == Common.AUTH_BASIC)
            {
                string raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration.Username}:{configuration.Password}"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
            }
            else if (configuration.AuthType == Common.AUTH_API_KEY)
            {
                message.Headers.TryAddWithoutValidation(string.IsNullOrWhiteSpace(configuration.ApiKeyHeader) ? "X-API-KEY" : configuration.ApiKeyHeader, configuration.ApiKey);
            }

            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                watch.Stop();
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                string? number = ReadDocumentNumber(body);
                bool unknown = !response.IsSuccessStatusCode && ((int)response.StatusCode >= 500 || (int)response.StatusCode == 408);
                if (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(number))
                {
                    number = fallbackDocumentNumber;
                }

                return new ExternalResult
                {
                    Succeeded = response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(number),
                    StatusCode = (int)response.StatusCode,
                    DocumentNumber = number,
                    Body = Trim(body),
                    OutcomeUnknown = unknown,
                    DurationMs = watch.ElapsedMilliseconds,
                    ErrorMessage = response.IsSuccessStatusCode
                        ? (string.IsNullOrWhiteSpace(ReadDocumentNumber(body)) ? "Supplier accepted the order. The response did not include a document number, so the external order reference was stored." : null)
                        : $"Supplier ERP returned HTTP {(int)response.StatusCode}."
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                watch.Stop();
                return new ExternalResult
                {
                    Succeeded = false,
                    StatusCode = 0,
                    OutcomeUnknown = true,
                    DurationMs = watch.ElapsedMilliseconds,
                    ErrorMessage = "Supplier ERP call did not complete. The order may already exist, so the same external order number must be reused."
                };
            }
        }

        private HttpClient CreateClient(SupplierErpIntegrationConfiguration configuration)
        {
            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds <= 0 ? 60 : configuration.TimeoutSeconds);
            return client;
        }

        private static void Apply(SupplierErpIntegrationConfiguration target, SupplierErpWriteDto request, bool keepSecrets)
        {
            target.ErpType = request.ErpType.Trim();
            target.BaseUrl = request.BaseUrl.Trim();
            target.AuthPath = request.AuthPath;
            target.OrderPath = request.OrderPath.Trim();
            target.HttpMethod = string.IsNullOrWhiteSpace(request.HttpMethod) ? "POST" : request.HttpMethod.Trim();
            target.AuthType = request.AuthType.Trim();
            target.TokenUrl = request.TokenUrl;
            target.Username = request.Username;
            target.ClientId = request.ClientId;
            target.Scope = request.Scope;
            target.ApiKeyHeader = request.ApiKeyHeader;
            target.DefaultShipTo = request.DefaultShipTo;
            target.OrderDateFormat = string.IsNullOrWhiteSpace(request.OrderDateFormat) ? "dd/MM/yyyy" : request.OrderDateFormat;
            target.HeadersJson = request.HeadersJson;
            target.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 60 : request.TimeoutSeconds;
            target.MaxRetryCount = request.MaxRetryCount < 0 ? 0 : request.MaxRetryCount;
            target.IsActive = request.IsActive;
            target.Password = Secret(request.Password, target.Password, keepSecrets);
            target.ClientSecret = Secret(request.ClientSecret, target.ClientSecret, keepSecrets);
            target.ApiKey = Secret(request.ApiKey, target.ApiKey, keepSecrets);
            target.AccessToken = Secret(request.AccessToken, target.AccessToken, keepSecrets);
        }

        private static string? Secret(string? incoming, string? current, bool keepSecrets)
        {
            if (!string.IsNullOrWhiteSpace(incoming))
            {
                return incoming;
            }

            return keepSecrets ? current : incoming;
        }

        private static string Combine(string baseUrl, string? path)
        {
            string root = baseUrl.TrimEnd('/');
            string relative = string.IsNullOrWhiteSpace(path) ? string.Empty : (path.StartsWith('/') ? path : "/" + path);
            return root + relative;
        }

        private static string? ReadToken(string body)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                foreach (string name in new[] { "access_token", "accessToken", "token", "jwt" })
                {
                    if (document.RootElement.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                    {
                        return value.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                return null;
            }

            return null;
        }

        private static string? ReadDocumentNumber(string body)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                return ReadNumber(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? ReadNumber(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (string name in new[] { "documentNumber", "purchaseOrderNumber", "poNumber", "orderNumber", "orderNo", "jdeOrderNumber", "salesOrderNumber", "number" })
            {
                if (element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    return value.GetString();
                }
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    string? nested = ReadNumber(property.Value);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }
            }

            return null;
        }

        private static string Trim(string body)
        {
            return body.Length <= 4000 ? body : body.Substring(0, 4000);
        }

        private sealed class ExternalResult
        {
            public bool Succeeded { get; set; }
            public int StatusCode { get; set; }
            public string? DocumentNumber { get; set; }
            public string? Body { get; set; }
            public string? ErrorMessage { get; set; }
            public bool OutcomeUnknown { get; set; }
            public long DurationMs { get; set; }

            public static ExternalResult Definite(int statusCode, string error)
            {
                return new ExternalResult
                {
                    Succeeded = false,
                    StatusCode = statusCode,
                    ErrorMessage = error,
                    OutcomeUnknown = false
                };
            }
        }
    }
}
