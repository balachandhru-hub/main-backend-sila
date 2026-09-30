using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services.Integration
{
    public interface IBuyerPurchaseDocumentGateway
    {
        Task<ExternalCallResult> CreateBuyerPurchaseDocumentAsync(
            ErpIntegrationConfiguration configuration,
            string resolvedBaseUrl,
            string resolvedPath,
            string resolvedMethod,
            BuyerPurchaseDocumentRequest request,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Sends a purchase document to the buyer ERP endpoint stored on the organization configuration.
    /// Wishlist code calls CreateBuyerPurchaseDocument, not a vendor-specific method.
    /// </summary>
    public class BuyerPurchaseDocumentGateway : IBuyerPurchaseDocumentGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerManager _logger;

        public BuyerPurchaseDocumentGateway(IHttpClientFactory httpClientFactory, ILoggerManager logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<ExternalCallResult> CreateBuyerPurchaseDocumentAsync(
            ErpIntegrationConfiguration configuration,
            string resolvedBaseUrl,
            string resolvedPath,
            string resolvedMethod,
            BuyerPurchaseDocumentRequest request,
            CancellationToken cancellationToken)
        {
            string endpoint = Combine(resolvedBaseUrl, resolvedPath);
            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds <= 0 ? 60 : configuration.TimeoutSeconds);

            using HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(resolvedMethod), endpoint);
            message.Headers.TryAddWithoutValidation(Common.IDEMPOTENCY_HEADER, request.IdempotencyKey);
            message.Headers.TryAddWithoutValidation("X-Correlation-Id", request.CorrelationId);
            ApplyExtraHeaders(message, configuration.HeadersJson);

            string payload = BuildPayload(configuration, request);
            string mediaType = UsesCxml(configuration) ? "application/xml" : "application/json";
            message.Content = new StringContent(payload, Encoding.UTF8, mediaType);

            ExternalCallResult authFailure = await ApplyAuthenticationAsync(client, message, configuration, cancellationToken);
            if (!authFailure.Succeeded && authFailure.StatusCode != 0)
            {
                return authFailure;
            }

            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                watch.Stop();
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                string? documentNumber = ExternalDocumentNumberReader.TryRead(body);
                bool succeeded = response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(documentNumber);
                bool unknown = !response.IsSuccessStatusCode && ((int)response.StatusCode >= 500 || (int)response.StatusCode == 408);
                _logger.LogInfo(
                    $"Buyer ERP call finished. WishlistId={request.WishlistId} BuyerOrganizationId={request.BuyerOrganizationId} " +
                    $"IntegrationType={Common.INTEGRATION_BUYER_ERP} ConfigurationId={configuration.Id} CorrelationId={request.CorrelationId} " +
                    $"StatusCode={(int)response.StatusCode} DurationMs={watch.ElapsedMilliseconds}");

                return new ExternalCallResult
                {
                    Succeeded = succeeded,
                    StatusCode = (int)response.StatusCode,
                    DocumentNumber = documentNumber,
                    ResponseBody = Trim(body),
                    OutcomeUnknown = unknown || (response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(documentNumber)),
                    ErrorMessage = succeeded
                        ? null
                        : response.IsSuccessStatusCode
                            ? "Buyer ERP responded successfully but did not include a document number."
                            : $"Buyer ERP returned HTTP {(int)response.StatusCode}.",
                    DurationMs = watch.ElapsedMilliseconds
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                watch.Stop();
                _logger.LogError(
                    $"Buyer ERP call failed before a document number was received. WishlistId={request.WishlistId} " +
                    $"ConfigurationId={configuration.Id} CorrelationId={request.CorrelationId} DurationMs={watch.ElapsedMilliseconds} Error={ex.Message}");
                return new ExternalCallResult
                {
                    Succeeded = false,
                    StatusCode = 0,
                    OutcomeUnknown = true,
                    ErrorMessage = "Buyer ERP call did not complete. The document may already exist, so this attempt is not retried automatically.",
                    DurationMs = watch.ElapsedMilliseconds
                };
            }
        }

        private async Task<ExternalCallResult> ApplyAuthenticationAsync(
            HttpClient client,
            HttpRequestMessage message,
            ErpIntegrationConfiguration configuration,
            CancellationToken cancellationToken)
        {
            string authType = configuration.AuthType ?? Common.AUTH_NONE;
            if (authType == Common.AUTH_NONE)
            {
                return new ExternalCallResult { Succeeded = true };
            }

            if (authType == Common.AUTH_BEARER)
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.AccessToken);
                return new ExternalCallResult { Succeeded = true };
            }

            if (authType == Common.AUTH_API_KEY)
            {
                string header = string.IsNullOrWhiteSpace(configuration.ApiKeyHeader) ? "X-API-KEY" : configuration.ApiKeyHeader;
                message.Headers.TryAddWithoutValidation(header, configuration.ApiKey);
                return new ExternalCallResult { Succeeded = true };
            }

            if (authType == Common.AUTH_BASIC)
            {
                string raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration.Username}:{configuration.Password}"));
                message.Headers.Authorization = new AuthenticationHeaderValue("Basic", raw);
                return new ExternalCallResult { Succeeded = true };
            }

            if (authType == Common.AUTH_OAUTH2_CLIENT_CREDENTIALS)
            {
                if (string.IsNullOrWhiteSpace(configuration.TokenUrl))
                {
                    return new ExternalCallResult
                    {
                        Succeeded = false,
                        StatusCode = 400,
                        ErrorMessage = "Buyer ERP OAuth token URL is not configured."
                    };
                }

                var form = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = configuration.ClientId ?? string.Empty,
                    ["client_secret"] = configuration.ClientSecret ?? string.Empty
                };
                if (!string.IsNullOrWhiteSpace(configuration.Scope))
                {
                    form["scope"] = configuration.Scope;
                }

                using HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, configuration.TokenUrl)
                {
                    Content = new FormUrlEncodedContent(form)
                };
                using HttpResponseMessage tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
                string tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return new ExternalCallResult
                    {
                        Succeeded = false,
                        StatusCode = (int)tokenResponse.StatusCode,
                        ErrorMessage = "Buyer ERP token request failed.",
                        OutcomeUnknown = (int)tokenResponse.StatusCode >= 500
                    };
                }

                string? token = ExternalDocumentNumberReader.TryReadToken(tokenBody);
                if (string.IsNullOrWhiteSpace(token))
                {
                    return new ExternalCallResult
                    {
                        Succeeded = false,
                        StatusCode = (int)tokenResponse.StatusCode,
                        ErrorMessage = "Buyer ERP token response did not include an access token."
                    };
                }

                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return new ExternalCallResult { Succeeded = true };
            }

            return new ExternalCallResult
            {
                Succeeded = false,
                StatusCode = 400,
                ErrorMessage = $"Buyer ERP authentication type '{authType}' is not supported."
            };
        }

        private static void ApplyExtraHeaders(HttpRequestMessage message, string? headersJson)
        {
            if (string.IsNullOrWhiteSpace(headersJson))
            {
                return;
            }

            try
            {
                Dictionary<string, string>? headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                if (headers == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, string> header in headers)
                {
                    if (!header.Key.Contains("authorization", StringComparison.OrdinalIgnoreCase)
                        && !header.Key.Contains("secret", StringComparison.OrdinalIgnoreCase)
                        && !header.Key.Contains("token", StringComparison.OrdinalIgnoreCase)
                        && !header.Key.Contains("key", StringComparison.OrdinalIgnoreCase))
                    {
                        message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }
            catch (JsonException)
            {
            }
        }

        private static bool UsesCxml(ErpIntegrationConfiguration configuration)
        {
            if (string.Equals(configuration.PayloadFormat, Common.PAYLOAD_CXML, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(configuration.ErpType, Common.ERP_TYPE_ARIBA, StringComparison.OrdinalIgnoreCase)
                || string.Equals(configuration.ErpType, Common.ERP_TYPE_SAP_S4, StringComparison.OrdinalIgnoreCase);
        }

        private static bool UsesAneJson(ErpIntegrationConfiguration configuration)
        {
            return string.Equals(configuration.ErpType, Common.ERP_TYPE_ANE_DCI, StringComparison.OrdinalIgnoreCase)
                && !UsesCxml(configuration);
        }

        private static string BuildPayload(ErpIntegrationConfiguration configuration, BuyerPurchaseDocumentRequest request)
        {
            if (UsesCxml(configuration))
            {
                return BuildCxml(request);
            }

            if (UsesAneJson(configuration))
            {
                return JsonSerializer.Serialize(new
                {
                    shipTo = request.ShipTo ?? string.Empty,
                    orderDate = (request.RequiredDate ?? DateTime.UtcNow).ToString("dd/MM/yyyy"),
                    purchaseOrderNo = request.BuyerDocumentNumber,
                    deliveryInstruction = request.DeliveryInstruction ?? string.Empty,
                    extOrderNo = request.WishlistId,
                    entries = request.Lines.Select(line => new
                    {
                        qty = line.Quantity,
                        sku = line.MaterialCode,
                        uom = line.UnitOfMeasure,
                        unitPrice = line.UnitPrice
                    })
                });
            }

            return JsonSerializer.Serialize(new
            {
                documentType = request.DocumentType,
                externalReference = request.WishlistId,
                idempotencyKey = request.IdempotencyKey,
                buyerOrganizationId = request.BuyerOrganizationId,
                buyerDocumentNumber = request.BuyerDocumentNumber,
                shipTo = request.ShipTo,
                outletCode = request.OutletCode,
                outletName = request.OutletName,
                currency = request.Currency,
                deliveryInstruction = request.DeliveryInstruction,
                requiredDate = request.RequiredDate,
                lines = request.Lines.Select(line => new
                {
                    materialCode = line.MaterialCode,
                    materialName = line.MaterialName,
                    quantity = line.Quantity,
                    unitOfMeasure = line.UnitOfMeasure,
                    unitPrice = line.UnitPrice,
                    currency = line.Currency
                })
            });
        }

        private static string BuildCxml(BuyerPurchaseDocumentRequest request)
        {
            string currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency;
            decimal total = request.Lines.Sum(line => line.Quantity * (line.UnitPrice ?? 0));
            StringBuilder xml = new StringBuilder();
            xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.Append("<cXML xml:lang=\"en-US\"><Request><OrderRequest>");
            xml.Append("<OrderRequestHeader orderID=\"").Append(Escape(request.BuyerDocumentNumber ?? request.WishlistId.ToString())).Append("\" type=\"new\">");
            xml.Append("<Total><Money currency=\"").Append(Escape(currency)).Append("\">").Append(total).Append("</Money></Total>");
            if (!string.IsNullOrWhiteSpace(request.ShipTo))
            {
                xml.Append("<ShipTo><Address><Name xml:lang=\"en\">").Append(Escape(request.ShipTo)).Append("</Name></Address></ShipTo>");
            }
            xml.Append("</OrderRequestHeader>");
            int lineNumber = 1;
            foreach (BuyerPurchaseLine line in request.Lines)
            {
                xml.Append("<ItemOut quantity=\"").Append(line.Quantity).Append("\" lineNumber=\"").Append(lineNumber).Append("\">");
                xml.Append("<ItemID><SupplierPartID>").Append(Escape(line.MaterialCode)).Append("</SupplierPartID></ItemID>");
                xml.Append("<ItemDetail><UnitPrice><Money currency=\"").Append(Escape(line.Currency ?? currency)).Append("\">")
                    .Append(line.UnitPrice ?? 0).Append("</Money></UnitPrice>");
                xml.Append("<Description xml:lang=\"en\">").Append(Escape(line.MaterialName)).Append("</Description>");
                xml.Append("<UnitOfMeasure>").Append(Escape(line.UnitOfMeasure ?? string.Empty)).Append("</UnitOfMeasure>");
                xml.Append("</ItemDetail></ItemOut>");
                lineNumber++;
            }
            xml.Append("</OrderRequest></Request></cXML>");
            return xml.ToString();
        }

        private static string Escape(string value)
        {
            return System.Security.SecurityElement.Escape(value) ?? string.Empty;
        }

        private static string Combine(string baseUrl, string path)
        {
            string root = baseUrl.TrimEnd('/');
            string relative = path.StartsWith('/') ? path : "/" + path;
            return root + relative;
        }

        private static string Trim(string body)
        {
            const int limit = 4000;
            return body.Length <= limit ? body : body.Substring(0, limit);
        }
    }
}
