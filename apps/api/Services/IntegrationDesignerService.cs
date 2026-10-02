using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public sealed class IntegrationDesignerService(
    SilaMeDbContext db,
    IHttpClientFactory httpClientFactory,
    ProtectedIntegrationCredentialStore credentials,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ITenantContextAccessor tenants)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
    private static readonly Regex SafePath = new(@"^[A-Za-z0-9_./$-]+$", RegexOptions.Compiled);

    public string CurrentEnvironment()
    {
        if (tenants.Current is { } context)
            return TenantOperationalDatabase.EnvironmentCode(context.EnvironmentType);
        return configuration["Sila:OperationalEnvironment"]
            ?? Environment.GetEnvironmentVariable("SILA_OPERATIONAL_ENVIRONMENT")
            ?? (hostEnvironment.IsProduction() ? "PROD" : "TEST");
    }

    public IntegrationDesignerCatalogResponse Catalog() => IntegrationDesignerCatalog.Catalog();

    public string Fingerprint(IntegrationDesignerDraft draft, ApiIntegrationConfiguration? stored = null)
    {
        var secretMaterial = string.Join('|',
            SecretHash(draft.Password, stored?.ProtectedPassword),
            SecretHash(draft.ClientSecret, stored?.ProtectedClientSecret),
            SecretHash(draft.BearerToken ?? draft.ApiKey, stored?.ProtectedBearerToken),
            SecretHash(draft.ClientId, stored?.ProtectedClientId),
            string.Join(',', (draft.Designer.Headers ?? []).Where(item => item.IsSecret).Select(item => $"{item.Key}={item.Value ?? (item.Configured ? "configured" : "")}")));
        var material = new Dictionary<string, object?>
        {
            ["system"] = draft.SystemKind.ToString(),
            ["process"] = draft.ProcessType.ToString(),
            ["protocol"] = draft.Protocol.ToString(),
            ["baseUrl"] = NormalizeUrl(draft.BaseUrl),
            ["servicePath"] = (draft.ServicePath ?? "").Trim().Trim('/'),
            ["resource"] = (draft.EntitySet ?? draft.ResourcePath ?? "").Trim().Trim('/'),
            ["entitySet"] = (draft.EntitySet ?? "").Trim(),
            ["method"] = (draft.HttpMethod ?? "GET").Trim().ToUpperInvariant(),
            ["auth"] = draft.AuthenticationType.ToString(),
            ["username"] = (draft.Username ?? "").Trim().ToUpperInvariant(),
            ["tokenEndpoint"] = draft.TokenEndpoint ?? "",
            ["tokenScope"] = draft.TokenScope ?? "",
            ["tokenHttpMethod"] = draft.Designer.TokenHttpMethod ?? "",
            ["tokenHeaders"] = draft.TokenHeaders ?? new Dictionary<string, string>(),
            ["tokenBodyKeys"] = (draft.TokenBody ?? new Dictionary<string, string>()).Keys.OrderBy(item => item).ToArray(),
            ["soap"] = new { draft.Designer.WsdlUrl, draft.Designer.SoapOperation, draft.Designer.SoapAction },
            ["safeTestPath"] = draft.Designer.SafeTestPath ?? "",
            ["csrf"] = new
            {
                draft.Designer.CsrfRequired,
                draft.Designer.CsrfFetchMethod,
                draft.Designer.CsrfFetchPath,
                draft.Designer.CsrfHeaderName,
                draft.Designer.CsrfHeaderValue,
                draft.Designer.CsrfResponseHeader,
            },
            ["headers"] = (draft.Designer.Headers ?? []).Select(item => item.Key).OrderBy(item => item).ToArray(),
            ["mappings"] = draft.Mappings.OrderBy(item => item.TargetField).ThenBy(item => item.SourceField).Select(item => $"{item.SourceKind}:{item.SourceStructure}:{item.SourceField}->{item.TargetField}:{item.DefaultValue}").ToArray(),
            ["response"] = (draft.ResponseMappings ?? draft.Designer.ResponseMappings ?? []).OrderBy(item => item.TargetField).Select(item => $"{item.SourcePath}->{item.TargetField}").ToArray(),
            ["routing"] = $"{draft.EntityCode}|{draft.CompanyCode}|{draft.Plant}|{draft.PropertyCode}|{draft.Priority}",
            ["secrets"] = Hash(secretMaterial),
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(material, JsonOptions)))).ToLowerInvariant();
    }

    public async Task<IntegrationDesignerTestResponse> TestDraftAsync(Guid organizationId, IntegrationDesignerDraft draft, string testType, Guid? configurationId, CancellationToken cancellationToken)
    {
        ValidateMatrix(draft);
        var stored = configurationId is null ? null : await db.ApiIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == configurationId && item.OrganizationId == organizationId, cancellationToken);
        ApplyStoredSecrets(draft, stored);
        using var client = httpClientFactory.CreateClient("api-integrations");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(draft.TimeoutSeconds, 5, 300));
        var started = DateTime.UtcNow;
        try
        {
            var result = testType.ToUpperInvariant() switch
            {
                "SCHEMA" => await LoadSchemaInternalAsync(client, draft, cancellationToken),
                "ENDPOINT" => await ValidateEndpointAsync(client, draft, cancellationToken),
                "MAPPING" => ValidateMappings(draft),
                "TRANSACTION" => throw new IntegrationException("TEST_TRANSACTION_NOT_IMPLEMENTED", "TEST TRANSACTION execution is reserved for Phase 2 and is not implemented."),
                _ => await TestAuthenticationAsync(client, draft, cancellationToken),
            };
            result = result with { Fingerprint = result.Success ? Fingerprint(draft, stored) : null, TestedAt = DateTime.UtcNow };
            await RecordHistoryAsync(configurationId, draft.Name, result.TestType, result, DateTime.UtcNow - started, cancellationToken);
            return result;
        }
        catch (IntegrationException exception)
        {
            var failed = new IntegrationDesignerTestResponse(false, exception.Message, testType.ToUpperInvariant(), exception.Status, DateTime.UtcNow, null, false, false, false, false, false, null, null, null);
            await RecordHistoryAsync(configurationId, draft.Name, failed.TestType, failed, DateTime.UtcNow - started, cancellationToken);
            return failed;
        }
    }

    public IntegrationRequestPreviewResponse Preview(IntegrationDesignerDraft draft, JsonElement? sample = null)
    {
        ValidateMatrix(draft);
        var url = BuildRuntimeUrl(draft);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(draft.Designer.ContentType)) headers["Content-Type"] = draft.Designer.ContentType!;
        if (!string.IsNullOrWhiteSpace(draft.Designer.Accept)) headers["Accept"] = draft.Designer.Accept!;
        foreach (var header in draft.Designer.Headers ?? [])
            headers[header.Key] = header.IsSecret ? "••••••••" : header.Value ?? (header.Configured ? "••••••••" : "");
        if (draft.AuthenticationType == IntegrationAuthenticationType.BASIC) headers["Authorization"] = "Basic ••••••••";
        if (draft.AuthenticationType is IntegrationAuthenticationType.BEARER_TOKEN or IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS or IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT)
            headers["Authorization"] = "Bearer ••••••••";
        if (draft.Designer.CsrfRequired) headers[draft.Designer.CsrfHeaderName ?? "X-CSRF-Token"] = "••••••••";
        var body = BuildPreviewBody(draft, sample);
        return new IntegrationRequestPreviewResponse(url, draft.HttpMethod.ToUpperInvariant(), headers, body, true, "PREVIEW ONLY — NOTHING HAS BEEN POSTED.");
    }

    public async Task<IntegrationConfigurationResponse> SaveValidatedAsync(Guid organizationId, Guid? id, IntegrationDesignerDraft draft, string? validatedBy, CancellationToken cancellationToken)
    {
        ValidateMatrix(draft);
        var stored = id is null ? null : await db.ApiIntegrationConfigurations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
        var expected = Fingerprint(draft, stored);
        if (string.IsNullOrWhiteSpace(draft.ValidationFingerprint) || !string.Equals(draft.ValidationFingerprint, expected, StringComparison.OrdinalIgnoreCase))
            throw new IntegrationException("SAVE_REQUIRES_TEST", "Save is enabled only after a successful connection test.");
        if (!Uri.TryCreate(draft.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            throw new IntegrationException("INVALID_BASE_URL", "Base URL must be an absolute HTTPS or HTTP URL.");
        await EnsureUniqueActiveRoutingAsync(organizationId, stored?.Id ?? Guid.Empty, draft, false, cancellationToken);
        var configuration = stored ?? new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, EntityCode = "ALL", Name = string.Empty, BaseUrl = string.Empty, CreatedAt = DateTime.UtcNow
        };
        ApplyDraft(configuration, draft, baseUri);
        configuration.EnvironmentCode = CurrentEnvironment();
        configuration.Status = IntegrationConfigurationStatus.VALIDATED;
        configuration.ValidationFingerprint = expected;
        configuration.ValidatedAt = DateTime.UtcNow;
        configuration.ValidatedBy = validatedBy;
        configuration.ValidationStatus = "VALIDATED";
        configuration.ConnectionStatus = "SUCCESS";
        configuration.TestedAt = DateTime.UtcNow;
        configuration.LastSuccessfulTestAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;
        if (stored is null) db.ApiIntegrationConfigurations.Add(configuration);
        await ReplaceMappingsAsync(configuration.Id, draft, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ToDesignerResponse(configuration);
    }

    public async Task<bool> ActivateAsync(Guid organizationId, Guid id, bool active, CancellationToken cancellationToken)
    {
        var config = await db.ApiIntegrationConfigurations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
        if (active)
        {
            if (config.Status is not IntegrationConfigurationStatus.VALIDATED and not IntegrationConfigurationStatus.TESTED and not IntegrationConfigurationStatus.ACTIVE)
                throw new IntegrationException("VALIDATION_REQUIRED", "Activate requires a saved, validated configuration.");
            if (config.Status == IntegrationConfigurationStatus.VALIDATED)
            {
                var draft = ToDraft(config);
                draft.Mappings = await db.ApiFieldMappings.AsNoTracking().Where(item => item.ConfigurationId == id)
                    .Select(item => new DesignerMappingInput
                    {
                        SourceKind = item.SourceKind, SourceStructure = item.SourceStructure, SourceField = item.SourceField,
                        TargetField = item.TargetField, IsCollection = item.IsCollection, DefaultValue = item.DefaultValue, NullPolicy = item.NullPolicy
                    }).ToListAsync(cancellationToken);
                if (!string.Equals(config.ValidationFingerprint, Fingerprint(draft, config), StringComparison.OrdinalIgnoreCase))
                    throw new IntegrationException("VALIDATION_STALE", "The configuration changed after the last successful test.");
                if (credentials.State(config) == "Missing" && config.AuthenticationType != IntegrationAuthenticationType.NONE)
                    throw new IntegrationException("SECRETS_REQUIRED", "Activate requires configured secrets.");
            }
            var draftForRouting = ToDraft(config);
            await EnsureUniqueActiveRoutingAsync(organizationId, id, draftForRouting, true, cancellationToken);
            config.Status = IntegrationConfigurationStatus.ACTIVE;
        }
        else
        {
            config.Status = IntegrationConfigurationStatus.DISABLED;
        }
        config.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return active;
    }

    public async Task<IntegrationConfigurationResponse> DuplicateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var source = await db.ApiIntegrationConfigurations.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
        var copy = new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = source.OrganizationId, OrganizationUnitId = source.OrganizationUnitId,
            EntityCode = source.EntityCode, Name = $"{source.Name} copy", Description = source.Description, SystemKind = source.SystemKind,
            ProcessType = source.ProcessType, Protocol = source.Protocol, BaseUrl = source.BaseUrl, ResourcePath = source.ResourcePath,
            ServicePath = source.ServicePath, EntitySet = source.EntitySet, HttpMethod = source.HttpMethod, EnvironmentCode = source.EnvironmentCode,
            Priority = source.Priority, CompanyCode = source.CompanyCode, Plant = source.Plant, PropertyCode = source.PropertyCode,
            DesignerJson = source.DesignerJson, AuthenticationType = source.AuthenticationType, Username = source.Username,
            ProtectedPassword = source.ProtectedPassword, ProtectedClientId = source.ProtectedClientId, ProtectedClientSecret = source.ProtectedClientSecret,
            ProtectedBearerToken = source.ProtectedBearerToken, TokenEndpoint = source.TokenEndpoint, TokenScope = source.TokenScope,
            TokenHeadersJson = source.TokenHeadersJson, TokenBodyJson = source.TokenBodyJson, TimeoutSeconds = source.TimeoutSeconds,
            RetryCount = source.RetryCount, PageSize = source.PageSize, ScheduleCron = source.ScheduleCron,
            Status = IntegrationConfigurationStatus.DRAFT, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.ApiIntegrationConfigurations.Add(copy);
        var mappings = await db.ApiFieldMappings.AsNoTracking().Where(item => item.ConfigurationId == id).ToListAsync(cancellationToken);
        foreach (var mapping in mappings)
        {
            db.ApiFieldMappings.Add(new ApiFieldMapping
            {
                Id = Guid.NewGuid(), ConfigurationId = copy.Id, SourceField = mapping.SourceField, TargetField = mapping.TargetField,
                SourceKind = mapping.SourceKind, SourceStructure = mapping.SourceStructure, IsCollection = mapping.IsCollection,
                Transformation = mapping.Transformation, NullPolicy = mapping.NullPolicy, DefaultValue = mapping.DefaultValue,
                IsValidated = false, UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return ToDesignerResponse(copy);
    }

    public IntegrationConfigurationResponse ToDesignerResponse(ApiIntegrationConfiguration item)
    {
        var designer = ParseDesigner(item.DesignerJson);
        designer = MaskDesigner(designer);
        return new IntegrationConfigurationResponse(
            item.Id, item.OrganizationId, item.OrganizationUnitId, item.EntityCode, item.Name,
            item.ProcessType, item.Protocol, item.BaseUrl, item.ResourcePath, item.AuthenticationType, item.Username,
            credentials.State(item), item.TimeoutSeconds, item.RetryCount, item.PageSize, item.WatermarkField,
            item.LastWatermark, item.LastAttemptAt, item.LastSuccessfulRunAt, item.NextRunAt, item.IsRunning,
            item.LastErrorSafe, item.ScheduleCron, item.Status, item.TestedAt, item.CreatedAt, item.UpdatedAt,
            item.SystemKind, item.Description, item.HttpMethod, item.ServicePath, item.EntitySet, item.Priority,
            item.CompanyCode, item.Plant, item.PropertyCode, item.EnvironmentCode, item.ValidationFingerprint,
            item.ValidatedAt, item.ValidatedBy, item.ValidationStatus, item.LastSuccessfulTestAt, designer, item.ConnectionStatus);
    }

    public static void ValidateMatrix(IntegrationDesignerDraft draft)
    {
        if (!IntegrationDesignerCatalog.InterfacesBySystem[draft.SystemKind].Contains(draft.Protocol))
            throw new IntegrationException("INTERFACE_NOT_SUPPORTED", "The selected interface type is not available for this system.");
        if (!IntegrationDesignerCatalog.AuthenticationFor(draft.SystemKind, draft.Protocol).Contains(draft.AuthenticationType))
            throw new IntegrationException("AUTH_NOT_SUPPORTED", "The selected authentication type is not available for this system and interface.");
        if (!string.IsNullOrWhiteSpace(draft.ResourcePath) && !SafePath.IsMatch(draft.ResourcePath.Trim('/')))
            throw new IntegrationException("INVALID_RESOURCE_PATH", "The resource path contains unsupported characters.");
        if (!string.IsNullOrWhiteSpace(draft.ServicePath) && !SafePath.IsMatch(draft.ServicePath.Trim('/')))
            throw new IntegrationException("INVALID_SERVICE_PATH", "The service path contains unsupported characters.");
        var recommendation = IntegrationDesignerCatalog.Recommendations().GetValueOrDefault(draft.ProcessType);
        if (recommendation is not null)
        {
            if ((draft.Designer.SelectedSourceStructures ?? []).Count == 0)
                draft.Designer = draft.Designer with { SelectedSourceStructures = recommendation.Primary.Concat(recommendation.Recommended).Distinct().ToList() };
        }
    }

    public async Task<IntegrationConfigurationResponse> SaveDraftAsync(Guid organizationId, Guid? id, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        ValidateMatrix(draft);
        if (!Uri.TryCreate(draft.BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme is not ("http" or "https"))
            throw new IntegrationException("INVALID_BASE_URL", "Base URL must be an absolute HTTPS or HTTP URL.");
        var stored = id is null ? null : await db.ApiIntegrationConfigurations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The integration configuration was not found.", 404);
        var configuration = stored ?? new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, EntityCode = "ALL", Name = string.Empty, BaseUrl = string.Empty, CreatedAt = DateTime.UtcNow
        };
        ApplyDraft(configuration, draft, baseUri);
        configuration.EnvironmentCode = CurrentEnvironment();
        configuration.Status = IntegrationConfigurationStatus.DRAFT;
        configuration.ValidationFingerprint = null;
        configuration.ValidatedAt = null;
        configuration.ValidatedBy = null;
        configuration.ValidationStatus = "NOT_TESTED";
        configuration.ConnectionStatus = null;
        configuration.UpdatedAt = DateTime.UtcNow;
        if (stored is null) db.ApiIntegrationConfigurations.Add(configuration);
        await db.SaveChangesAsync(cancellationToken);
        return ToDesignerResponse(configuration);
    }

    private async Task<IntegrationDesignerTestResponse> TestAuthenticationAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        var tokenAcquired = false;
        var csrfAcquired = false;
        var sessionEstablished = false;
        if (draft.AuthenticationType is IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS or IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT)
        {
            tokenAcquired = await AcquireTokenAsync(client, draft, cancellationToken) is not null;
            if (!tokenAcquired) return Fail("AUTH", "The token endpoint did not return an access token.", 401);
            if (!draft.Designer.CsrfRequired)
                return new IntegrationDesignerTestResponse(true, "Authentication successful. Token acquired.", "AUTH", 200, DateTime.UtcNow, null, true, true, false, false, false, null, null, null);
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, SafeProbeUrl(draft));
        await ApplyAuthenticationAsync(client, request, draft, cancellationToken);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return Fail("AUTH", "Authentication failed.", (int)response.StatusCode);
        if (draft.Designer.CsrfRequired)
        {
            using var csrfRequest = new HttpRequestMessage(ParseMethod(draft.Designer.CsrfFetchMethod ?? "GET"), IntegrationODataUrl.CsrfFetchUrl(draft));
            await ApplyAuthenticationAsync(client, csrfRequest, draft, cancellationToken);
            csrfRequest.Headers.TryAddWithoutValidation(draft.Designer.CsrfHeaderName ?? "X-CSRF-Token", draft.Designer.CsrfHeaderValue ?? "Fetch");
            using var csrfResponse = await client.SendAsync(csrfRequest, cancellationToken);
            var csrfHeader = draft.Designer.CsrfResponseHeader ?? "X-CSRF-Token";
            csrfAcquired = HeaderValues(csrfResponse, csrfHeader).Any(item => !string.IsNullOrWhiteSpace(item));
            sessionEstablished = HeaderValues(csrfResponse, "Set-Cookie").Any();
            if (!csrfAcquired) return Fail("AUTH", "CSRF token was not returned.", (int)csrfResponse.StatusCode);
        }
        var message = draft.Designer.CsrfRequired
            ? $"Authentication successful. CSRF acquired{(sessionEstablished ? ". Session established" : "")}."
            : "Authentication successful.";
        return new IntegrationDesignerTestResponse(true, message, "AUTH", (int)response.StatusCode, DateTime.UtcNow, null, true, tokenAcquired, csrfAcquired, sessionEstablished, false, null, null, null);
    }

    private async Task<IntegrationDesignerTestResponse> ValidateEndpointAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        if (draft.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4)
            return await LoadSchemaInternalAsync(client, draft, cancellationToken);
        if (draft.Protocol == IntegrationProtocol.SOAP)
        {
            var wsdlUrl = draft.Designer.WsdlUrl ?? draft.BaseUrl;
            using var request = new HttpRequestMessage(HttpMethod.Get, wsdlUrl);
            await ApplyAuthenticationAsync(client, request, draft, cancellationToken);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return Fail("ENDPOINT", "The WSDL/service could not be validated.", (int)response.StatusCode);
            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var operations = IntegrationDesignerSchemaParser.ParseWsdl(xml);
            if (!string.IsNullOrWhiteSpace(draft.Designer.SoapOperation) && operations.All(item => !item.Name.Equals(draft.Designer.SoapOperation, StringComparison.OrdinalIgnoreCase)))
                return Fail("ENDPOINT", "The configured SOAP operation was not found in the WSDL.", 400);
            return new IntegrationDesignerTestResponse(true, "SOAP service validated.", "ENDPOINT", (int)response.StatusCode, DateTime.UtcNow, null, true, false, false, false, true, "WSDL", DateTime.UtcNow, operations);
        }
        var probe = draft.Designer.OpenApiUrl ?? draft.BaseUrl;
        using var restRequest = new HttpRequestMessage(HttpMethod.Get, probe);
        await ApplyAuthenticationAsync(client, restRequest, draft, cancellationToken);
        using var restResponse = await client.SendAsync(restRequest, cancellationToken);
        if (restResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return Fail("ENDPOINT", "The endpoint rejected authentication.", (int)restResponse.StatusCode);
        return new IntegrationDesignerTestResponse(true, "REST endpoint validated using a safe GET.", "ENDPOINT", (int)restResponse.StatusCode, DateTime.UtcNow, null, true, false, false, false, false, "REST", DateTime.UtcNow, null);
    }

    private async Task<IntegrationDesignerTestResponse> LoadSchemaInternalAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        IReadOnlyList<IntegrationSchemaEntity> entities;
        string source;
        int status;
        if (draft.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4)
        {
            var metadataUrl = IntegrationODataUrl.MetadataUrl(draft.BaseUrl, draft.ServicePath, draft.EntitySet);
            using var request = new HttpRequestMessage(HttpMethod.Get, metadataUrl);
            await ApplyAuthenticationAsync(client, request, draft, cancellationToken);
            IntegrationRequestCapture.AcceptXml(request);
            using var response = await client.SendAsync(request, cancellationToken);
            status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode) return Fail("SCHEMA", "OData metadata could not be loaded.", status);
            entities = IntegrationDesignerSchemaParser.ParseOData(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!string.IsNullOrWhiteSpace(draft.EntitySet) && entities.All(item => item.EntitySet != draft.EntitySet && item.Name != draft.EntitySet))
                return Fail("SCHEMA", $"Entity set {draft.EntitySet} was not found in $metadata.", 400);
            source = "$metadata";
        }
        else if (draft.Protocol == IntegrationProtocol.SOAP)
        {
            var wsdlUrl = draft.Designer.WsdlUrl ?? draft.BaseUrl;
            using var request = new HttpRequestMessage(HttpMethod.Get, wsdlUrl);
            await ApplyAuthenticationAsync(client, request, draft, cancellationToken);
            using var response = await client.SendAsync(request, cancellationToken);
            status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(draft.Designer.SamplePayload))
                return Fail("SCHEMA", "WSDL could not be loaded.", status);
            var xml = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(cancellationToken) : draft.Designer.SamplePayload ?? "";
            entities = xml.TrimStart().StartsWith('<') && xml.Contains("definitions", StringComparison.OrdinalIgnoreCase)
                ? IntegrationDesignerSchemaParser.ParseWsdl(xml)
                : IntegrationDesignerSchemaParser.ParseXmlSample(xml);
            source = "WSDL";
        }
        else if (!string.IsNullOrWhiteSpace(draft.Designer.OpenApiUrl))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, draft.Designer.OpenApiUrl);
            await ApplyAuthenticationAsync(client, request, draft, cancellationToken);
            using var response = await client.SendAsync(request, cancellationToken);
            status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode) return Fail("SCHEMA", "OpenAPI schema could not be loaded.", status);
            entities = IntegrationDesignerSchemaParser.ParseOpenApi(await response.Content.ReadAsStringAsync(cancellationToken));
            source = "OpenAPI";
        }
        else if (!string.IsNullOrWhiteSpace(draft.Designer.SamplePayload))
        {
            entities = IntegrationDesignerSchemaParser.ParseJsonSample(draft.Designer.SamplePayload!);
            status = 200;
            source = "SAMPLE_JSON";
        }
        else
        {
            return Fail("SCHEMA", "Provide an OpenAPI URL or a sample request to derive a REST schema.", 400);
        }
        return new IntegrationDesignerTestResponse(true, "Schema loaded successfully.", "SCHEMA", status, DateTime.UtcNow, null, true, false, false, false, true, source, DateTime.UtcNow, entities);
    }

    private static IntegrationDesignerTestResponse ValidateMappings(IntegrationDesignerDraft draft)
    {
        if (draft.Designer.RequestBodyMode == IntegrationRequestBodyMode.FIELD_MAPPING && draft.Mappings.Count == 0)
            return Fail("MAPPING", "At least one field mapping is required.", 400);
        var structures = draft.Designer.SelectedSourceStructures ?? [];
        var invalid = draft.Mappings.Where(item => item.SourceKind == IntegrationMappingSourceKind.FIELD && !string.IsNullOrWhiteSpace(item.SourceStructure) && !structures.Contains(item.SourceStructure)).ToList();
        if (invalid.Count > 0) return Fail("MAPPING", "Mappings must use selected source structures.", 400);
        return new IntegrationDesignerTestResponse(true, "Mapping configuration is valid.", "MAPPING", 200, DateTime.UtcNow, null, true, false, false, false, false, null, null, null);
    }

    private async Task ApplyAuthenticationAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, CancellationToken cancellationToken, bool skipToken = false)
    {
        switch (draft.AuthenticationType)
        {
            case IntegrationAuthenticationType.BASIC:
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{draft.Username}:{draft.Password}")));
                break;
            case IntegrationAuthenticationType.BEARER_TOKEN:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", draft.BearerToken);
                break;
            case IntegrationAuthenticationType.API_KEY:
                request.Headers.TryAddWithoutValidation(draft.Designer.ApiKeyHeader ?? "APIKey", draft.ApiKey ?? draft.BearerToken ?? "");
                break;
            case IntegrationAuthenticationType.CUSTOM_HEADER:
                request.Headers.TryAddWithoutValidation(draft.Designer.CustomHeaderName ?? "X-API-Key", draft.ApiKey ?? draft.BearerToken ?? "");
                break;
            case IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS:
            case IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT:
                if (skipToken) break;
                var token = await AcquireTokenAsync(client, draft, cancellationToken);
                if (string.IsNullOrWhiteSpace(token)) throw new IntegrationException("TOKEN_RESPONSE_INVALID", "The token endpoint did not return an access token.");
                request.Headers.Authorization = new AuthenticationHeaderValue(draft.Designer.TokenType ?? "Bearer", token);
                break;
        }
    }

    private async Task<string?> AcquireTokenAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draft.TokenEndpoint)) throw new IntegrationException("TOKEN_ENDPOINT_REQUIRED", "A token endpoint is required for this authentication type.");
        using var tokenRequest = new HttpRequestMessage(ParseMethod(draft.Designer.TokenHttpMethod ?? "POST"), draft.TokenEndpoint);
        foreach (var header in draft.TokenHeaders ?? []) tokenRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        var body = new Dictionary<string, string>(draft.TokenBody ?? new Dictionary<string, string>());
        if (draft.AuthenticationType == IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS)
        {
            body["grant_type"] = body.GetValueOrDefault("grant_type") ?? "client_credentials";
            body["client_id"] = draft.ClientId ?? "";
            body["client_secret"] = draft.ClientSecret ?? "";
            if (!string.IsNullOrWhiteSpace(draft.TokenScope)) body["scope"] = draft.TokenScope!;
        }
        tokenRequest.Content = new FormUrlEncodedContent(body);
        using var tokenResponse = await client.SendAsync(tokenRequest, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode) return null;
        using var json = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));
        return ReadPath(json.RootElement, draft.Designer.TokenResponsePath ?? "access_token");
    }

    private void ApplyDraft(ApiIntegrationConfiguration configuration, IntegrationDesignerDraft draft, Uri baseUri)
    {
        configuration.Name = draft.Name.Trim();
        configuration.Description = draft.Description?.Trim();
        configuration.SystemKind = draft.SystemKind;
        configuration.ProcessType = draft.ProcessType;
        configuration.Protocol = draft.Protocol;
        configuration.BaseUrl = baseUri.ToString().TrimEnd('/');
        configuration.ServicePath = draft.ServicePath?.Trim('/');
        configuration.EntitySet = draft.EntitySet?.Trim();
        configuration.ResourcePath = string.IsNullOrWhiteSpace(draft.ResourcePath)
            ? string.Join('/', new[] { configuration.ServicePath, configuration.EntitySet }.Where(item => !string.IsNullOrWhiteSpace(item)))
            : draft.ResourcePath.Trim('/');
        configuration.HttpMethod = string.IsNullOrWhiteSpace(draft.HttpMethod) ? "GET" : draft.HttpMethod.Trim().ToUpperInvariant();
        configuration.AuthenticationType = draft.AuthenticationType;
        configuration.Username = draft.Username?.Trim();
        configuration.ProtectedPassword = credentials.Protect(draft.Password) ?? configuration.ProtectedPassword;
        configuration.ProtectedClientId = credentials.Protect(draft.ClientId) ?? configuration.ProtectedClientId;
        configuration.ProtectedClientSecret = credentials.Protect(draft.ClientSecret) ?? configuration.ProtectedClientSecret;
        configuration.ProtectedBearerToken = credentials.Protect(draft.BearerToken ?? draft.ApiKey) ?? configuration.ProtectedBearerToken;
        configuration.TokenEndpoint = draft.TokenEndpoint;
        configuration.TokenScope = draft.TokenScope;
        configuration.TokenHeadersJson = draft.TokenHeaders is null ? configuration.TokenHeadersJson : JsonSerializer.Serialize(draft.TokenHeaders, JsonOptions);
        configuration.TokenBodyJson = draft.TokenBody is null ? configuration.TokenBodyJson : JsonSerializer.Serialize(draft.TokenBody, JsonOptions);
        configuration.TimeoutSeconds = draft.TimeoutSeconds;
        configuration.RetryCount = draft.RetryCount;
        configuration.ScheduleCron = draft.ScheduleCron;
        configuration.OrganizationUnitId = draft.OrganizationUnitId;
        configuration.EntityCode = string.IsNullOrWhiteSpace(draft.EntityCode) ? "ALL" : draft.EntityCode.Trim();
        configuration.Priority = draft.Priority;
        configuration.CompanyCode = draft.CompanyCode?.Trim();
        configuration.Plant = draft.Plant?.Trim();
        configuration.PropertyCode = draft.PropertyCode?.Trim();
        var designer = MaskDesignerForStore(draft.Designer with { ResponseMappings = draft.ResponseMappings });
        configuration.DesignerJson = JsonSerializer.Serialize(designer, JsonOptions);
    }

    public IntegrationDesignerDraft RuntimeDraft(ApiIntegrationConfiguration config)
    {
        var draft = ToDraft(config);
        ApplyStoredSecrets(draft, config);
        return draft;
    }

    private IntegrationDesignerDraft ToDraft(ApiIntegrationConfiguration config) => new()
    {
        Name = config.Name,
        Description = config.Description,
        SystemKind = config.SystemKind,
        ProcessType = config.ProcessType,
        Protocol = config.Protocol,
        BaseUrl = config.BaseUrl,
        ResourcePath = config.ResourcePath,
        ServicePath = config.ServicePath,
        EntitySet = config.EntitySet,
        HttpMethod = config.HttpMethod,
        AuthenticationType = config.AuthenticationType,
        Username = config.Username,
        TokenEndpoint = config.TokenEndpoint,
        TokenScope = config.TokenScope,
        TokenHeaders = string.IsNullOrWhiteSpace(config.TokenHeadersJson) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(config.TokenHeadersJson, JsonOptions),
        TokenBody = string.IsNullOrWhiteSpace(config.TokenBodyJson) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(config.TokenBodyJson, JsonOptions),
        TimeoutSeconds = config.TimeoutSeconds,
        RetryCount = config.RetryCount,
        ScheduleCron = config.ScheduleCron,
        OrganizationUnitId = config.OrganizationUnitId,
        EntityCode = config.EntityCode,
        Priority = config.Priority,
        CompanyCode = config.CompanyCode,
        Plant = config.Plant,
        PropertyCode = config.PropertyCode,
        Designer = ParseDesigner(config.DesignerJson),
        ResponseMappings = ParseDesigner(config.DesignerJson).ResponseMappings?.ToList() ?? [],
        ValidationFingerprint = config.ValidationFingerprint,
    };

    private async Task ReplaceMappingsAsync(Guid configurationId, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        await db.ApiFieldMappings.Where(item => item.ConfigurationId == configurationId).ExecuteDeleteAsync(cancellationToken);
        var now = DateTime.UtcNow;
        db.ApiFieldMappings.AddRange(draft.Mappings.Select(input => new ApiFieldMapping
        {
            Id = Guid.NewGuid(), ConfigurationId = configurationId,
            SourceField = string.IsNullOrWhiteSpace(input.SourceField) ? input.DefaultValue ?? "" : input.SourceField.Trim(),
            TargetField = input.TargetField.Trim(),
            SourceKind = input.SourceKind,
            SourceStructure = input.SourceStructure,
            IsCollection = input.IsCollection || (input.TargetField.Contains("[]", StringComparison.Ordinal) || input.SourceStructure?.EndsWith("_ITEM", StringComparison.OrdinalIgnoreCase) == true),
            DefaultValue = input.DefaultValue,
            NullPolicy = input.SourceKind == IntegrationMappingSourceKind.DEFAULT ? IntegrationNullPolicy.DEFAULT_VALUE : input.NullPolicy,
            IsValidated = true, UpdatedAt = now
        }));
    }

    private async Task EnsureUniqueActiveRoutingAsync(Guid organizationId, Guid excludeId, IntegrationDesignerDraft draft, bool activating, CancellationToken cancellationToken)
    {
        if (!activating) return;
        var duplicate = await db.ApiIntegrationConfigurations.AnyAsync(item =>
            item.Id != excludeId && item.OrganizationId == organizationId && item.ProcessType == draft.ProcessType &&
            item.Status == IntegrationConfigurationStatus.ACTIVE &&
            item.EntityCode == (string.IsNullOrWhiteSpace(draft.EntityCode) ? "ALL" : draft.EntityCode.Trim()) &&
            item.CompanyCode == draft.CompanyCode && item.Plant == draft.Plant && item.PropertyCode == draft.PropertyCode &&
            item.Priority == draft.Priority &&
            (item.EnvironmentCode == null || item.EnvironmentCode == CurrentEnvironment()), cancellationToken);
        if (duplicate) throw new IntegrationException("ROUTING_AMBIGUOUS", "An ACTIVE configuration already exists for this API type, routing combination, and priority.");
    }

    private async Task RecordHistoryAsync(Guid? configurationId, string name, string testType, IntegrationDesignerTestResponse result, TimeSpan duration, CancellationToken cancellationToken)
    {
        if (configurationId is null) return;
        if (!await db.ApiIntegrationConfigurations.AnyAsync(item => item.Id == configurationId, cancellationToken)) return;
        db.ApiIntegrationExecutions.Add(new ApiIntegrationExecution
        {
            Id = Guid.NewGuid(), ConfigurationId = configurationId.Value, Trigger = IntegrationExecutionTrigger.TEST,
            Status = result.Success ? IntegrationExecutionStatus.SUCCESS : IntegrationExecutionStatus.FAILED,
            StartedAt = DateTime.UtcNow - duration, CompletedAt = DateTime.UtcNow,
            ErrorCode = result.Success ? null : "TEST_FAILED", ErrorMessageSafe = result.Message,
            DetailJson = JsonSerializer.Serialize(new { testType, name, result.HttpStatus, durationMs = (int)duration.TotalMilliseconds, result.AuthenticationSuccessful, result.TokenAcquired, result.CsrfAcquired, result.SchemaSource }, JsonOptions)
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public void MergeStoredSecrets(IntegrationDesignerDraft draft, ApiIntegrationConfiguration? stored) =>
        ApplyStoredSecrets(draft, stored);

    private void ApplyStoredSecrets(IntegrationDesignerDraft draft, ApiIntegrationConfiguration? stored)
    {
        if (stored is null) return;
        draft.Password ??= credentials.Unprotect(stored.ProtectedPassword);
        draft.ClientSecret ??= credentials.Unprotect(stored.ProtectedClientSecret);
        draft.BearerToken ??= credentials.Unprotect(stored.ProtectedBearerToken);
        draft.ClientId ??= credentials.Unprotect(stored.ProtectedClientId);
        draft.ApiKey ??= credentials.Unprotect(stored.ProtectedBearerToken);
    }

    private IntegrationDesignerDocument ParseDesigner(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new IntegrationDesignerDocument() : JsonSerializer.Deserialize<IntegrationDesignerDocument>(json, JsonOptions) ?? new IntegrationDesignerDocument();

    private static IntegrationDesignerDocument MaskDesigner(IntegrationDesignerDocument designer) => designer with
    {
        Headers = (designer.Headers ?? []).Select(item => new IntegrationHeaderInput { Key = item.Key, IsSecret = item.IsSecret, Configured = item.IsSecret || item.Configured, Value = item.IsSecret ? null : item.Value }).ToList(),
        SamplePayload = designer.SamplePayload,
        RequestTemplate = designer.RequestTemplate,
    };

    private IntegrationDesignerDocument MaskDesignerForStore(IntegrationDesignerDocument designer)
    {
        var headers = (designer.Headers ?? []).Select(item => new IntegrationHeaderInput
        {
            Key = item.Key, IsSecret = item.IsSecret,
            Configured = item.IsSecret && (!string.IsNullOrWhiteSpace(item.Value) || item.Configured),
            Value = item.IsSecret ? null : item.Value
        }).ToList();
        return designer with { Headers = headers };
    }

    private static string BuildRuntimeUrl(IntegrationDesignerDraft draft)
    {
        var path = string.Join('/', new[] { draft.ServicePath, draft.EntitySet ?? draft.ResourcePath }.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!.Trim('/')));
        if (string.IsNullOrWhiteSpace(path)) return draft.BaseUrl.TrimEnd('/');
        return IntegrationODataUrl.Combine(draft.BaseUrl, path);
    }

    private static IEnumerable<string> HeaderValues(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var headers)) return headers;
        if (response.Content.Headers.TryGetValues(name, out var contentHeaders)) return contentHeaders;
        return [];
    }

    private static string SafeProbeUrl(IntegrationDesignerDraft draft) =>
        draft.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4
            ? IntegrationODataUrl.MetadataUrl(draft.BaseUrl, draft.ServicePath, draft.EntitySet)
            : draft.Designer.WsdlUrl ?? draft.BaseUrl;

    private static string BuildPreviewBody(IntegrationDesignerDraft draft, JsonElement? sample)
    {
        if (draft.Designer.RequestBodyMode is IntegrationRequestBodyMode.JSON_TEMPLATE or IntegrationRequestBodyMode.XML_TEMPLATE)
            return draft.Designer.RequestTemplate ?? "";
        var header = new Dictionary<string, object?>();
        var collections = new Dictionary<string, List<Dictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in draft.Mappings)
        {
            var value = mapping.SourceKind switch
            {
                IntegrationMappingSourceKind.CONSTANT => mapping.DefaultValue ?? mapping.SourceField,
                IntegrationMappingSourceKind.DEFAULT => mapping.DefaultValue,
                _ => PreviewSourceValue(mapping, sample)
            };
            if (mapping.IsCollection || mapping.TargetField.Contains("[]", StringComparison.Ordinal))
            {
                var parts = mapping.TargetField.Split("[]", 2);
                var collection = parts[0];
                var child = parts.Length > 1 ? parts[1].Trim('.') : mapping.TargetField;
                if (!collections.TryGetValue(collection, out var rows)) { rows = [new Dictionary<string, object?>()]; collections[collection] = rows; }
                rows[0][child] = value;
            }
            else header[mapping.TargetField] = value;
        }
        foreach (var collection in collections) header[collection.Key] = collection.Value;
        return JsonSerializer.Serialize(header, JsonOptions);
    }

    private static object? PreviewSourceValue(DesignerMappingInput mapping, JsonElement? sample)
    {
        if (sample is null) return $"{{{{{mapping.SourceStructure}.{mapping.SourceField}}}}}";
        if (sample.Value.ValueKind == JsonValueKind.Object && sample.Value.TryGetProperty(mapping.SourceField, out var value))
            return value.ToString();
        return $"{{{{{mapping.SourceStructure}.{mapping.SourceField}}}}}";
    }

    private static string NormalizeUrl(string url) => url.Trim().TrimEnd('/');
    private string SecretHash(string? provided, string? protectedValue) => Hash(provided ?? credentials.Unprotect(protectedValue));
    private static string Hash(string? value) => string.IsNullOrEmpty(value) ? "" : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static HttpMethod ParseMethod(string method) => method.ToUpperInvariant() switch { "POST" => HttpMethod.Post, "PUT" => HttpMethod.Put, "PATCH" => HttpMethod.Patch, "DELETE" => HttpMethod.Delete, _ => HttpMethod.Get };
    private static IntegrationDesignerTestResponse Fail(string type, string message, int? status) =>
        new(false, message, type, status, DateTime.UtcNow, null, false, false, false, false, false, null, null, null);
    private static string? ReadPath(JsonElement element, string path)
    {
        var current = element;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current)) return null;
        }
        return current.ValueKind is JsonValueKind.String or JsonValueKind.Number ? current.ToString() : current.GetRawText();
    }
}
