using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services.Integration
{
    public interface IIntegrationHttpExecutor
    {
        /// <summary>URL of the configured resource. A test call reads one record only.</summary>
        string BuildUrl(ApiIntegrationConfiguration configuration, bool testOnly);

        string MetadataUrl(ApiIntegrationConfiguration configuration);

        /// <summary>Sends the request with the configured authentication, timeout and retries.</summary>
        Task<HttpResponseMessage> SendAsync(ApiIntegrationConfiguration configuration, string url, HttpMethod method, string? body, CancellationToken cancellationToken);

        List<IntegrationSchemaEntityDto> ParseMetadata(string xml);

        List<JsonElement> ReadRecords(string payload);

        /// <summary>Posts a goods receipt to the ERP. Never throws: failures are returned in the result.</summary>
        Task<ErpGoodsReceiptResult> PostGoodsReceiptAsync(ApiIntegrationConfiguration configuration, GoodsReceipt goodsReceipt, List<GoodsReceiptLine> lines, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Outbound HTTP executor of the ERP integration framework (REST and OData V4).
    /// It talks to the remote system only; what is imported or stored is decided by the handlers.
    /// </summary>
    public class IntegrationHttpExecutor : IIntegrationHttpExecutor
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIntegrationCredentialProtector _credentials;
        private readonly ILoggerManager _logger;

        public IntegrationHttpExecutor(IHttpClientFactory httpClientFactory, IIntegrationCredentialProtector credentials, ILoggerManager logger)
        {
            _httpClientFactory = httpClientFactory;
            _credentials = credentials;
            _logger = logger;
        }

        public string BuildUrl(ApiIntegrationConfiguration configuration, bool testOnly)
        {
            string url = Combine(configuration.BaseUrl, configuration.ResourcePath ?? string.Empty);
            if (configuration.Protocol == IntegrationProtocol.ODATA_V4)
            {
                string query = testOnly ? "$top=1" : $"$top={configuration.PageSize ?? 100}";
                if (!testOnly && !string.IsNullOrWhiteSpace(configuration.WatermarkField) && configuration.LastWatermark != null)
                {
                    query += $"&$filter={Uri.EscapeDataString(configuration.WatermarkField)}%20gt%20{Uri.EscapeDataString(configuration.LastWatermark.Value.ToUniversalTime().ToString("O"))}";
                }

                url += (url.Contains('?') ? "&" : "?") + query;
            }

            return url;
        }

        public string MetadataUrl(ApiIntegrationConfiguration configuration)
        {
            return Combine(configuration.BaseUrl, "$metadata");
        }

        public async Task<HttpResponseMessage> SendAsync(ApiIntegrationConfiguration configuration, string url, HttpMethod method, string? body, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_INTEGRATIONS);
            client.Timeout = TimeSpan.FromSeconds(configuration.TimeoutSeconds <= 0 ? 30 : configuration.TimeoutSeconds);
            int attempts = Math.Max(1, configuration.RetryCount + 1);
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                using HttpRequestMessage request = new HttpRequestMessage(method, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                await AddAuthenticationAsync(configuration, request, client, cancellationToken);
                if (body != null)
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if ((int)response.StatusCode >= 500 && attempt < attempts)
                    {
                        response.Dispose();
                        await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
                        continue;
                    }

                    return response;
                }
                catch (HttpRequestException) when (attempt < attempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < attempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
                }
                catch (HttpRequestException exception)
                {
                    _logger.LogError($"Integration endpoint could not be reached. ConfigurationId: {configuration.Id}, Error: {exception.Message}");
                    throw new IntegrationException("REMOTE_UNAVAILABLE", "The configured API could not be reached.", 424);
                }
                catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError($"Integration endpoint timed out. ConfigurationId: {configuration.Id}");
                    throw new IntegrationException("REMOTE_TIMEOUT", "The configured API did not answer in time.", 424);
                }
            }

            throw new IntegrationException("REMOTE_UNAVAILABLE", "The configured API could not be reached.", 424);
        }

        public List<IntegrationSchemaEntityDto> ParseMetadata(string xml)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(xml);
            }
            catch (XmlException)
            {
                throw new IntegrationException("SCHEMA_INVALID", "The OData metadata document is not valid XML.");
            }

            return document.Descendants().Where(item => item.Name.LocalName == "EntityType").Select(entity => new IntegrationSchemaEntityDto
            {
                Name = (string?)entity.Attribute("Name") ?? string.Empty,
                Properties = entity.Elements().Where(item => item.Name.LocalName == "Property").Select(property => new IntegrationSchemaPropertyDto
                {
                    Name = (string?)property.Attribute("Name") ?? string.Empty,
                    Type = ((string?)property.Attribute("Type") ?? "Edm.String").Replace("Edm.", string.Empty),
                    Nullable = (string?)property.Attribute("Nullable") != "false"
                }).ToList(),
                Keys = entity.Elements().Where(item => item.Name.LocalName == "Key").Elements()
                    .Select(item => (string?)item.Attribute("Name") ?? string.Empty)
                    .Where(item => item.Length > 0)
                    .ToList()
            }).ToList();
        }

        public List<JsonElement> ReadRecords(string payload)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return document.RootElement.EnumerateArray().Select(item => item.Clone()).ToList();
                }

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("value", out JsonElement value) && value.ValueKind == JsonValueKind.Array)
                {
                    return value.EnumerateArray().Select(item => item.Clone()).ToList();
                }

                return new List<JsonElement> { document.RootElement.Clone() };
            }
            catch (JsonException)
            {
                throw new IntegrationException("REMOTE_PAYLOAD_INVALID", "The API did not return valid JSON.", 424);
            }
        }

        public async Task<ErpGoodsReceiptResult> PostGoodsReceiptAsync(ApiIntegrationConfiguration configuration, GoodsReceipt goodsReceipt, List<GoodsReceiptLine> lines, CancellationToken cancellationToken)
        {
            string payload = JsonSerializer.Serialize(new
            {
                goodsReceipt.GrnNumber,
                goodsReceipt.ReceiptDate,
                goodsReceipt.PurchaseOrderId,
                goodsReceipt.SupplierId,
                Lines = lines.Select(line => new
                {
                    line.PurchaseOrderItemId,
                    line.MaterialCode,
                    line.ReceivedQuantity,
                    line.AcceptedQuantity,
                    line.DamagedQuantity,
                    line.RejectedQuantity,
                    line.Uom,
                    line.BatchNumber,
                    line.ExpiryDate,
                }),
            }, JsonOptions);
            try
            {
                using HttpResponseMessage response = await SendAsync(configuration, BuildUrl(configuration, false), HttpMethod.Post, payload, cancellationToken);
                string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                string safeResponse = responseJson.Length > 20000 ? responseJson[..20000] : responseJson;
                if (!response.IsSuccessStatusCode)
                {
                    return new ErpGoodsReceiptResult
                    {
                        Configured = true,
                        HttpStatus = (int)response.StatusCode,
                        ResponseJson = safeResponse,
                        ErrorCode = "ERP_POST_FAILED",
                        ErrorMessage = "The ERP rejected the goods receipt."
                    };
                }

                return new ErpGoodsReceiptResult
                {
                    Configured = true,
                    Success = true,
                    HttpStatus = (int)response.StatusCode,
                    MaterialDocument = ReadProperty(responseJson, "MaterialDocument", "materialDocument", "MaterialDocumentNumber"),
                    DocumentYear = ReadProperty(responseJson, "DocumentYear", "documentYear"),
                    ResponseJson = safeResponse
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Unknown(null, "ERP_POST_UNKNOWN", "The ERP response timed out.");
            }
            catch (HttpRequestException)
            {
                return Unknown(null, "ERP_POST_UNKNOWN", "The ERP could not be reached.");
            }
            catch (IntegrationException exception)
            {
                return Unknown(exception.Status, exception.Code, exception.Message);
            }
        }

        private static ErpGoodsReceiptResult Unknown(int? status, string code, string message)
        {
            return new ErpGoodsReceiptResult { Configured = true, Unknown = true, HttpStatus = status, ErrorCode = code, ErrorMessage = message };
        }

        private static string? ReadProperty(string responseJson, params string[] names)
        {
            try
            {
                using JsonDocument json = JsonDocument.Parse(responseJson);
                if (json.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                foreach (string name in names)
                {
                    if (json.RootElement.TryGetProperty(name, out JsonElement value))
                    {
                        return value.ToString();
                    }
                }
            }
            catch (JsonException)
            {
                // The ERP accepted the receipt but answered with a body that is not JSON.
                return null;
            }

            return null;
        }

        private async Task AddAuthenticationAsync(ApiIntegrationConfiguration configuration, HttpRequestMessage request, HttpClient client, CancellationToken cancellationToken)
        {
            switch (configuration.AuthenticationType)
            {
                case IntegrationAuthenticationType.BASIC:
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuration.Username}:{_credentials.Unprotect(configuration.ProtectedPassword)}")));
                    break;
                case IntegrationAuthenticationType.BEARER_TOKEN:
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _credentials.Unprotect(configuration.ProtectedBearerToken));
                    break;
                case IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS:
                case IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT:
                {
                    if (string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
                    {
                        throw new IntegrationException("TOKEN_ENDPOINT_REQUIRED", "A token endpoint is required for this authentication type.");
                    }

                    using HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint);
                    tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _credentials.Unprotect(configuration.ProtectedClientId) ?? string.Empty,
                        ["client_secret"] = _credentials.Unprotect(configuration.ProtectedClientSecret) ?? string.Empty,
                        ["scope"] = configuration.TokenScope ?? string.Empty
                    });
                    using HttpResponseMessage tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
                    if (!tokenResponse.IsSuccessStatusCode)
                    {
                        throw new IntegrationException("TOKEN_REQUEST_FAILED", "The token endpoint rejected the configured credentials.", 424);
                    }

                    using JsonDocument tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
                    if (!tokenJson.RootElement.TryGetProperty("access_token", out JsonElement token))
                    {
                        throw new IntegrationException("TOKEN_RESPONSE_INVALID", "The token endpoint did not return an access token.", 424);
                    }

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.GetString());
                    break;
                }
            }
        }

        private static string Combine(string baseUrl, string path)
        {
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }
    }
}
