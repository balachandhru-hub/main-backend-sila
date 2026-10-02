using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Integration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Validation and field mapping of an integration configuration, shared by the create and
    /// update commands, plus the lookup every integration use case starts with.
    /// </summary>
    internal static class IntegrationConfigurationRules
    {
        private static readonly Regex SafePath = new Regex(@"^[A-Za-z0-9_./$-]+$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public const string PAYLOAD_JSON = "JSON";
        public const string PAYLOAD_SOAP = "SOAP";
        public const string PAYLOAD_CXML = "CXML";
        public const string DEFAULT_API_KEY_HEADER = "X-API-KEY";

        private static readonly string[] HttpMethods = { "GET", "POST", "PUT", "PATCH" };
        private static readonly string[] PayloadFormats = { PAYLOAD_JSON, PAYLOAD_SOAP, PAYLOAD_CXML };

        // Credentials belong in the sign-in fields, where they are stored encrypted.
        private static readonly string[] ReservedHeaderWords = { "authorization", "secret", "token", "key" };

        public static async Task<ApiIntegrationConfiguration> GetTrackedAsync(IRepositoryWrapper repository, ILoggerManager logger, Guid configurationId, Guid organizationId)
        {
            ApiIntegrationConfiguration? configuration = await repository.ApiIntegrationConfiguration.FindFirstByConditionAsync(
                x => x.Id == configurationId && x.OrganizationId == organizationId && x.IsActive);
            if (configuration == null)
            {
                logger.LogError($"Integration configuration not found. ConfigurationId: {configurationId}, OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            return configuration;
        }

        public static async Task ApplyAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials,
            ApiIntegrationConfiguration configuration,
            IntegrationConfigurationInputDto input,
            string? organizationType,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.EntityCode))
            {
                logger.LogError($"Integration name or entity code is missing. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Name and entity code are required.", "Enter the name and the entity code of the integration.");
            }

            if (!string.IsNullOrWhiteSpace(input.ResourcePath) && !SafePath.IsMatch(input.ResourcePath))
            {
                logger.LogError($"Integration resource path is invalid. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Invalid resource path.", "The resource path contains unsupported characters.");
            }

            // A buyer configures the buyer's API types and a supplier the supplier's; both use this same configuration.
            IntegrationProcessCatalog.ProcessInfo? process = IntegrationProcessCatalog.Find(input.ProcessType);
            if (process == null || !IntegrationProcessCatalog.IsAvailableTo(input.ProcessType, organizationType))
            {
                logger.LogError($"Integration process is not available. ProcessType: {input.ProcessType}, OrganizationType: {organizationType}");
                throw new BadRequestCustomException("API type is not available.", "This API type is not available for your organization.");
            }

            // An API type that is read is called with GET; one the application sends to defaults to POST.
            string httpMethod = string.IsNullOrWhiteSpace(input.HttpMethod)
                ? (process.IsPush ? "POST" : "GET")
                : input.HttpMethod.Trim().ToUpperInvariant();
            string payloadFormat = string.IsNullOrWhiteSpace(input.PayloadFormat) ? PAYLOAD_JSON : input.PayloadFormat.Trim().ToUpperInvariant();
            if (!HttpMethods.Contains(httpMethod) || !PayloadFormats.Contains(payloadFormat))
            {
                logger.LogError($"Integration method or payload format is invalid. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Invalid method or payload format.", "Use GET, POST, PUT or PATCH, and JSON, SOAP or CXML.");
            }

            string? requestBody = string.IsNullOrWhiteSpace(input.RequestBody) ? null : input.RequestBody;
            if (payloadFormat != PAYLOAD_JSON && requestBody == null)
            {
                logger.LogError($"Integration body template is missing. PayloadFormat: {payloadFormat}, OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Request body is required.", "SOAP and cXML calls need the request body saved on this API.");
            }

            if (input.Headers != null && input.Headers.Keys.Any(name =>
                    string.IsNullOrWhiteSpace(name) || ReservedHeaderWords.Any(word => name.Contains(word, StringComparison.OrdinalIgnoreCase))))
            {
                logger.LogError($"Integration header is not allowed. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Header is not allowed.", "Credentials go in the sign-in fields, not in the extra headers.");
            }

            if (!Uri.TryCreate(input.BaseUrl, UriKind.Absolute, out Uri? baseUri) || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                logger.LogError($"Integration base URL is invalid. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Invalid base URL.", "Base URL must be an absolute HTTPS or HTTP URL.");
            }

            // An organization has one API per API type. The existing one is edited instead of adding a second.
            bool duplicate = await repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id != configuration.Id && x.OrganizationId == configuration.OrganizationId
                    && x.ProcessType == input.ProcessType)
                .AnyAsync(cancellationToken);
            if (duplicate)
            {
                logger.LogError($"Integration already exists. ProcessType: {input.ProcessType}, OrganizationId: {configuration.OrganizationId}");
                throw new ConflictCustomException("Integration already exists.", "An API of this type is already configured. Open it and edit it instead of creating another.");
            }

            await OperationsScope.EnsureUnitAsync(repository, logger, configuration.OrganizationId, input.OrganizationUnitId, cancellationToken);

            configuration.OrganizationUnitId = input.OrganizationUnitId;
            configuration.EntityCode = input.EntityCode.Trim();
            configuration.Name = input.Name.Trim();
            configuration.ProcessType = input.ProcessType;
            configuration.Protocol = input.Protocol;
            configuration.BaseUrl = baseUri.ToString().TrimEnd('/');
            configuration.ResourcePath = input.ResourcePath?.Trim('/');
            configuration.AuthenticationType = input.AuthenticationType;
            configuration.Username = input.Username?.Trim();

            // A credential that is not sent again keeps its stored (encrypted) value.
            configuration.ProtectedPassword = credentials.Protect(input.Password) ?? configuration.ProtectedPassword;
            configuration.ProtectedClientId = credentials.Protect(input.ClientId) ?? configuration.ProtectedClientId;
            configuration.ProtectedClientSecret = credentials.Protect(input.ClientSecret) ?? configuration.ProtectedClientSecret;
            configuration.ProtectedBearerToken = credentials.Protect(input.BearerToken) ?? configuration.ProtectedBearerToken;
            configuration.TokenEndpoint = input.TokenEndpoint;
            configuration.TokenScope = input.TokenScope;
            configuration.TokenHeadersJson = input.TokenHeaders == null ? configuration.TokenHeadersJson : JsonSerializer.Serialize(input.TokenHeaders, JsonOptions);
            configuration.TokenBodyJson = input.TokenBody == null ? configuration.TokenBodyJson : JsonSerializer.Serialize(input.TokenBody, JsonOptions);
            configuration.TimeoutSeconds = input.TimeoutSeconds;
            configuration.RetryCount = input.RetryCount;
            configuration.PageSize = input.PageSize;
            configuration.WatermarkField = input.WatermarkField;
            configuration.ScheduleCron = input.ScheduleCron;
            configuration.SystemName = string.IsNullOrWhiteSpace(input.SystemName) ? null : input.SystemName.Trim();
            configuration.HttpMethod = httpMethod;
            configuration.PayloadFormat = payloadFormat;
            configuration.RequestBody = requestBody;
            configuration.HeadersJson = input.Headers == null || input.Headers.Count == 0 ? null : JsonSerializer.Serialize(input.Headers, JsonOptions);
            configuration.ApiKeyHeader = string.IsNullOrWhiteSpace(input.ApiKeyHeader) ? null : input.ApiKeyHeader.Trim();
            configuration.ProtectedApiKey = credentials.Protect(input.ApiKey) ?? configuration.ProtectedApiKey;
        }

        /// <summary>The extra headers saved on a configuration, or null when there are none.</summary>
        public static Dictionary<string, string>? ReadHeaders(string? headersJson)
        {
            if (string.IsNullOrWhiteSpace(headersJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson, JsonOptions);
            }
            catch (JsonException)
            {
                // Headers saved in a shape this version cannot read are treated as none.
                return null;
            }
        }
    }
}
