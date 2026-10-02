using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public sealed class IntegrationConnectionTestService(
    SilaMeDbContext db,
    IHttpMessageHandlerFactory handlerFactory,
    IntegrationDesignerService designer,
    IntegrationAuthenticationResolver authentication,
    CsrfSessionProvider csrf,
    IHostEnvironment hostEnvironment,
    ITenantContextAccessor tenants)
{
    public static readonly TimeSpan ProofLifetime = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IntegrationConnectionTestResult> TestConnectionAsync(
        Guid organizationId,
        Guid? configurationId,
        Guid? userId,
        IntegrationDesignerDraft draft,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        var checks = new List<IntegrationConnectionCheck>();
        try
        {
            IntegrationDesignerService.ValidateMatrix(draft);
            ValidateRequiredFields(draft);
            var stored = configurationId is null ? null : await db.ApiIntegrationConfigurations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == configurationId && item.OrganizationId == organizationId, cancellationToken);
            designer.MergeStoredSecrets(draft, stored);
            EnsureUrls(draft);
            checks.Add(new IntegrationConnectionCheck("HOST", true));

            using var client = CreateSessionClient(draft.TimeoutSeconds);
            var context = new IntegrationAuthContext();
            var handler = authentication.Resolve(draft.AuthenticationType);
            async Task ApplyAuthAsync(HttpRequestMessage request) =>
                await handler.AuthenticateAsync(client, request, draft, context, cancellationToken);

            if (draft.AuthenticationType is IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS or IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT)
            {
                using var probe = new HttpRequestMessage(HttpMethod.Get, SafeServiceUrl(draft));
                await ApplyAuthAsync(probe);
                checks.Add(new IntegrationConnectionCheck("TOKEN", true));
                checks.Add(new IntegrationConnectionCheck("AUTHENTICATION", true));
            }
            else if (draft.AuthenticationType != IntegrationAuthenticationType.NONE)
            {
                checks.Add(new IntegrationConnectionCheck("AUTHENTICATION", true));
            }

            CsrfSession? session = null;
            if (draft.Designer.CsrfRequired)
            {
                session = await csrf.FetchAsync(client, draft, ApplyAuthAsync, cancellationToken);
                checks.Add(new IntegrationConnectionCheck("CSRF", true));
                checks.Add(new IntegrationConnectionCheck("SESSION", session.CookiesPresent, session.CookiesPresent ? null : "SESSION_COOKIE_OPTIONAL"));
            }

            await ValidateServiceAsync(client, draft, ApplyAuthAsync, session, checks, cancellationToken);
            var fingerprint = designer.Fingerprint(draft, stored);
            var testedAt = DateTime.UtcNow;
            var expiresAt = testedAt.Add(ProofLifetime);
            var record = new IntegrationConnectionTest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TenantId = tenants.Current?.TenantId,
                UserId = userId,
                ConfigurationId = configurationId,
                Name = draft.Name.Trim(),
                Fingerprint = fingerprint,
                SystemKind = draft.SystemKind.ToString(),
                ProcessType = draft.ProcessType.ToString(),
                Protocol = draft.Protocol.ToString(),
                Success = true,
                Status = "CONNECTION_SUCCESSFUL",
                ChecksJson = JsonSerializer.Serialize(checks, JsonOptions),
                DurationMs = (int)(testedAt - started).TotalMilliseconds,
                TestedAt = testedAt,
                ExpiresAt = expiresAt,
            };
            db.IntegrationConnectionTests.Add(record);
            await RecordHistoryAsync(configurationId, draft, record, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return new IntegrationConnectionTestResult(true, record.Id, record.Status, record.SystemKind, record.Protocol, checks, testedAt, expiresAt, null, "Connection successful.", fingerprint);
        }
        catch (Exception exception)
        {
            var mapped = MapException(exception);
            if (!checks.Any(item => !item.Success))
                checks.Add(new IntegrationConnectionCheck(mapped.Check, false, mapped.Code));
            var testedAt = DateTime.UtcNow;
            var record = new IntegrationConnectionTest
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TenantId = tenants.Current?.TenantId,
                UserId = userId,
                ConfigurationId = configurationId,
                Name = string.IsNullOrWhiteSpace(draft.Name) ? "unsaved" : draft.Name.Trim(),
                Fingerprint = "",
                SystemKind = draft.SystemKind.ToString(),
                ProcessType = draft.ProcessType.ToString(),
                Protocol = draft.Protocol.ToString(),
                Success = false,
                Status = "CONNECTION_FAILED",
                ErrorCode = mapped.Code,
                ErrorMessageSafe = mapped.Message,
                ChecksJson = JsonSerializer.Serialize(checks, JsonOptions),
                HttpStatus = mapped.Status,
                DurationMs = (int)(testedAt - started).TotalMilliseconds,
                TestedAt = testedAt,
                ExpiresAt = testedAt,
            };
            db.IntegrationConnectionTests.Add(record);
            await RecordHistoryAsync(configurationId, draft, record, cancellationToken, mapped.Detail);
            await db.SaveChangesAsync(cancellationToken);
            return new IntegrationConnectionTestResult(false, null, record.Status, record.SystemKind, record.Protocol, checks, testedAt, null, mapped.Code, mapped.Message, null, mapped.Detail);
        }
    }

    public async Task EnsureValidProofAsync(Guid organizationId, Guid? userId, IntegrationDesignerDraft draft, Guid? configurationId, CancellationToken cancellationToken)
    {
        if (draft.TestId is null || draft.TestId == Guid.Empty)
            throw new IntegrationException("SAVE_REQUIRES_TEST", "Save is enabled only after a successful connection test.");
        var proof = await db.IntegrationConnectionTests.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == draft.TestId, cancellationToken)
            ?? throw new IntegrationException("SAVE_REQUIRES_TEST", "Save is enabled only after a successful connection test.");
        if (proof.OrganizationId != organizationId || (proof.TenantId is not null && tenants.Current is { } tenant && proof.TenantId != tenant.TenantId))
            throw new IntegrationException("TEST_TENANT_MISMATCH", "The connection test does not belong to this customer.", 403);
        if (proof.UserId is not null && userId is not null && proof.UserId != userId)
            throw new IntegrationException("TEST_TENANT_MISMATCH", "The connection test does not belong to this user.", 403);
        if (!proof.Success || proof.ExpiresAt <= DateTime.UtcNow)
            throw new IntegrationException("TEST_EXPIRED", "The connection test expired. Test the connection again.");
        var stored = configurationId is null ? null : await db.ApiIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == configurationId && item.OrganizationId == organizationId, cancellationToken);
        designer.MergeStoredSecrets(draft, stored);
        var fingerprint = designer.Fingerprint(draft, stored);
        if (!string.Equals(proof.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
            throw new IntegrationException("CONFIGURATION_CHANGED_RETEST_REQUIRED", "The configuration changed after the last successful test.");
        draft.ValidationFingerprint = fingerprint;
    }

    private async Task ValidateServiceAsync(
        HttpClient client,
        IntegrationDesignerDraft draft,
        Func<HttpRequestMessage, Task> applyAuthentication,
        CsrfSession? session,
        List<IntegrationConnectionCheck> checks,
        CancellationToken cancellationToken)
    {
        if (draft.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4)
        {
            var url = IntegrationODataUrl.MetadataUrl(draft.BaseUrl, draft.ServicePath, draft.EntitySet);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            await applyAuthentication(request);
            if (session is not null)
                request.Headers.TryAddWithoutValidation(draft.Designer.CsrfHeaderName ?? "X-CSRF-Token", session.Token);
            IntegrationRequestCapture.AcceptXml(request);
            if (draft.Protocol == IntegrationProtocol.ODATA_V2)
            {
                request.Headers.TryAddWithoutValidation("DataServiceVersion", "2.0");
                request.Headers.TryAddWithoutValidation("MaxDataServiceVersion", "2.0");
            }
            using var response = await SendSafeAsync(request, client, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new IntegrationException("SERVICE_NOT_FOUND", "The OData service could not be validated.", (int)response.StatusCode, IntegrationRequestCapture.Json(request, url, (int)response.StatusCode, body));
            checks.Add(new IntegrationConnectionCheck("SERVICE", true));
            IReadOnlyList<IntegrationSchemaEntity> entities;
            try
            {
                entities = IntegrationDesignerSchemaParser.ParseOData(body);
            }
            catch
            {
                throw new IntegrationException("SERVICE_NOT_FOUND", "The OData service could not be validated.", (int)response.StatusCode, IntegrationRequestCapture.Json(request, url, (int)response.StatusCode, body));
            }
            if (!string.IsNullOrWhiteSpace(draft.EntitySet) && entities.All(item => item.EntitySet != draft.EntitySet && item.Name != draft.EntitySet))
                throw new IntegrationException("ENTITY_SET_NOT_FOUND", $"Entity set {draft.EntitySet} was not found.", 404, IntegrationRequestCapture.Json(request, url, (int)response.StatusCode, body));
            checks.Add(new IntegrationConnectionCheck("ENTITY SET", true));
            return;
        }

        if (draft.Protocol == IntegrationProtocol.SOAP)
        {
            using var endpoint = new HttpRequestMessage(HttpMethod.Get, draft.BaseUrl);
            await applyAuthentication(endpoint);
            using var endpointResponse = await SendSafeAsync(endpoint, client, cancellationToken);
            if (endpointResponse.StatusCode is HttpStatusCode.Unauthorized)
                throw new IntegrationException("AUTHENTICATION_FAILED", "Authentication failed.", 401);
            checks.Add(new IntegrationConnectionCheck("SOAP SERVICE", endpointResponse.IsSuccessStatusCode || (int)endpointResponse.StatusCode is >= 200 and < 500));
            if (string.IsNullOrWhiteSpace(draft.Designer.WsdlUrl))
                return;
            using var wsdl = new HttpRequestMessage(HttpMethod.Get, draft.Designer.WsdlUrl);
            await applyAuthentication(wsdl);
            using var wsdlResponse = await SendSafeAsync(wsdl, client, cancellationToken);
            if (!wsdlResponse.IsSuccessStatusCode)
                throw new IntegrationException("WSDL_NOT_FOUND", "The WSDL could not be loaded.", (int)wsdlResponse.StatusCode);
            var operations = IntegrationDesignerSchemaParser.ParseWsdl(await wsdlResponse.Content.ReadAsStringAsync(cancellationToken));
            checks.Add(new IntegrationConnectionCheck("WSDL", true));
            if (!string.IsNullOrWhiteSpace(draft.Designer.SoapOperation) && operations.All(item => !item.Name.Equals(draft.Designer.SoapOperation, StringComparison.OrdinalIgnoreCase)))
                throw new IntegrationException("SOAP_OPERATION_NOT_FOUND", "The configured SOAP operation was not found in the WSDL.");
            return;
        }

        using var rest = new HttpRequestMessage(HttpMethod.Get, SafeServiceUrl(draft));
        await applyAuthentication(rest);
        if (session is not null)
            rest.Headers.TryAddWithoutValidation(draft.Designer.CsrfHeaderName ?? "X-CSRF-Token", session.Token);
        using var restResponse = await SendSafeAsync(rest, client, cancellationToken);
        if (restResponse.StatusCode is HttpStatusCode.Unauthorized)
            throw new IntegrationException("AUTHENTICATION_FAILED", "Authentication failed.", 401);
        if (restResponse.StatusCode is HttpStatusCode.Forbidden)
            throw new IntegrationException("TOKEN_REJECTED", "The business API rejected the token.", 403);
        if (!restResponse.IsSuccessStatusCode && restResponse.StatusCode != HttpStatusCode.NotFound)
            throw new IntegrationException("CONNECTION_TEST_FAILED", "The business API could not be validated safely.", (int)restResponse.StatusCode);
        checks.Add(new IntegrationConnectionCheck("SERVICE", true));
    }

    private HttpClient CreateSessionClient(int timeoutSeconds)
    {
        var inner = handlerFactory.CreateHandler("api-integrations");
        var session = new CookieSessionHandler(inner);
        return new HttpClient(session, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds <= 0 ? 30 : timeoutSeconds, 5, 300)),
        };
    }

    private static async Task<HttpResponseMessage> SendSafeAsync(HttpRequestMessage request, HttpClient client, CancellationToken cancellationToken)
    {
        if (request.Method.Method is "POST" or "PUT" or "PATCH" or "DELETE"
            && request.RequestUri?.AbsolutePath.Contains("$metadata", StringComparison.OrdinalIgnoreCase) != true
            && request.RequestUri?.AbsolutePath.Contains("oauth", StringComparison.OrdinalIgnoreCase) != true
            && request.RequestUri?.AbsolutePath.Contains("token", StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new IntegrationException("CONNECTION_TEST_FAILED", "Connection tests cannot execute write methods.");
        }
        return await client.SendAsync(request, cancellationToken);
    }

    private void EnsureUrls(IntegrationDesignerDraft draft)
    {
        IntegrationOutboundGuard.EnsureSafe(draft.BaseUrl, hostEnvironment, "Base URL");
        if (!string.IsNullOrWhiteSpace(draft.TokenEndpoint))
            IntegrationOutboundGuard.EnsureSafe(draft.TokenEndpoint, hostEnvironment, "Token URL");
        if (!string.IsNullOrWhiteSpace(draft.Designer.WsdlUrl))
            IntegrationOutboundGuard.EnsureSafe(draft.Designer.WsdlUrl, hostEnvironment, "WSDL URL");
        if (!string.IsNullOrWhiteSpace(draft.Designer.SafeTestPath) && Uri.TryCreate(draft.Designer.SafeTestPath, UriKind.Absolute, out _))
            IntegrationOutboundGuard.EnsureSafe(draft.Designer.SafeTestPath, hostEnvironment, "Safe test URL");
    }

    private static void ValidateRequiredFields(IntegrationDesignerDraft draft)
    {
        if (string.IsNullOrWhiteSpace(draft.Name)) throw new IntegrationException("INVALID_CONFIGURATION", "Configuration name is required.");
        if (string.IsNullOrWhiteSpace(draft.BaseUrl)) throw new IntegrationException("INVALID_CONFIGURATION", "Base URL is required.");
        if (draft.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4)
        {
            if (string.IsNullOrWhiteSpace(draft.ServicePath)) throw new IntegrationException("INVALID_CONFIGURATION", "Service path is required.");
            if (string.IsNullOrWhiteSpace(draft.EntitySet)) throw new IntegrationException("INVALID_CONFIGURATION", "Entity set is required.");
        }
    }

    private static string SafeServiceUrl(IntegrationDesignerDraft draft)
    {
        if (!string.IsNullOrWhiteSpace(draft.Designer.SafeTestPath))
            return Uri.TryCreate(draft.Designer.SafeTestPath, UriKind.Absolute, out _)
                ? draft.Designer.SafeTestPath!
                : IntegrationODataUrl.Combine(draft.BaseUrl, draft.Designer.SafeTestPath!);
        if (draft.Protocol is IntegrationProtocol.ODATA_V2 or IntegrationProtocol.ODATA_V4)
            return IntegrationODataUrl.MetadataUrl(draft.BaseUrl, draft.ServicePath, draft.EntitySet);
        if (draft.Protocol == IntegrationProtocol.SOAP)
            return draft.Designer.WsdlUrl ?? draft.BaseUrl;
        var path = draft.HttpMethod is "POST" or "PUT" or "PATCH" or "DELETE" ? "" : draft.ResourcePath;
        return string.IsNullOrWhiteSpace(path) ? draft.BaseUrl.TrimEnd('/') : IntegrationODataUrl.Combine(draft.BaseUrl, path);
    }

    private async Task RecordHistoryAsync(Guid? configurationId, IntegrationDesignerDraft draft, IntegrationConnectionTest record, CancellationToken cancellationToken, string? requestJson = null)
    {
        if (configurationId is null) return;
        if (!await db.ApiIntegrationConfigurations.AnyAsync(item => item.Id == configurationId, cancellationToken)) return;
        db.ApiIntegrationExecutions.Add(new ApiIntegrationExecution
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId.Value,
            Trigger = IntegrationExecutionTrigger.TEST,
            Status = record.Success ? IntegrationExecutionStatus.SUCCESS : IntegrationExecutionStatus.FAILED,
            StartedAt = record.TestedAt.AddMilliseconds(-record.DurationMs),
            CompletedAt = record.TestedAt,
            ErrorCode = record.ErrorCode,
            ErrorMessageSafe = record.ErrorMessageSafe,
            DetailJson = JsonSerializer.Serialize(new
            {
                testType = "CONNECTION",
                draft.Name,
                record.Status,
                record.HttpStatus,
                record.DurationMs,
                record.SystemKind,
                record.Protocol,
                request = requestJson,
            }, JsonOptions),
        });
    }

    private static (string Code, string Message, int? Status, string Check, string? Detail) MapException(Exception exception)
    {
        if (exception is IntegrationException integration)
        {
            var check = integration.Code switch
            {
                "AUTHENTICATION_FAILED" or "AUTHORIZATION_FAILED" => "AUTHENTICATION",
                "TOKEN_ENDPOINT_FAILED" or "TOKEN_EXTRACTION_FAILED" or "TOKEN_REJECTED" => "TOKEN",
                "CSRF_FETCH_FAILED" or "CSRF_TOKEN_MISSING" => "CSRF",
                "SESSION_FAILED" => "SESSION",
                "ENTITY_SET_NOT_FOUND" => "ENTITY SET",
                "WSDL_NOT_FOUND" or "SOAP_OPERATION_NOT_FOUND" => "WSDL",
                "UNSAFE_TARGET" => "HOST",
                _ => "SERVICE",
            };
            return (integration.Code, integration.Message, integration.Status, check, integration.Detail);
        }
        if (exception is TaskCanceledException) return ("CONNECTION_TIMEOUT", "The connection timed out.", 408, "HOST", null);
        if (exception is HttpRequestException http)
        {
            if (http.InnerException is AuthenticationException) return ("TLS_FAILED", "TLS negotiation failed.", null, "HOST", null);
            if (http.Message.Contains("No such host", StringComparison.OrdinalIgnoreCase) || http.Message.Contains("Name or service", StringComparison.OrdinalIgnoreCase))
                return ("DNS_RESOLUTION_FAILED", "The host name could not be resolved.", null, "HOST", null);
            return ("HOST_UNREACHABLE", "The host could not be reached.", null, "HOST", null);
        }
        return ("CONNECTION_TEST_FAILED", "The connection test failed.", null, "SERVICE", null);
    }
}

public sealed class CookieSessionHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
{
    private readonly CookieContainer cookies = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } uri)
        {
            var header = cookies.GetCookieHeader(uri);
            if (!string.IsNullOrWhiteSpace(header))
                request.Headers.TryAddWithoutValidation("Cookie", header);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (request.RequestUri is { } responseUri && response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            foreach (var value in values)
                StoreCookie(responseUri, value);
        }
        return response;
    }

    private void StoreCookie(Uri uri, string raw)
    {
        try
        {
            cookies.SetCookies(uri, raw);
            return;
        }
        catch (CookieException)
        {
        }

        var pair = raw.Split(';', 2)[0];
        var separator = pair.IndexOf('=');
        if (separator <= 0) return;
        try
        {
            cookies.Add(new Cookie(pair[..separator].Trim(), pair[(separator + 1)..].Trim(), "/", uri.Host)
            {
                Secure = uri.Scheme == Uri.UriSchemeHttps,
            });
        }
        catch (CookieException)
        {
        }
    }

    protected override void Dispose(bool disposing)
    {
        // The inner handler is owned by IHttpClientFactory and must not be disposed.
    }
}
