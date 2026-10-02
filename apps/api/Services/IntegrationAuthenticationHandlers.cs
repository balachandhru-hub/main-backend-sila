using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class IntegrationAuthContext
{
    public string? AccessToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
}

public interface IIntegrationAuthenticationHandler
{
    IntegrationAuthenticationType Type { get; }
    Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken);
}

public sealed class IntegrationAuthenticationResolver(IEnumerable<IIntegrationAuthenticationHandler> handlers)
{
    public IIntegrationAuthenticationHandler Resolve(IntegrationAuthenticationType type) =>
        handlers.FirstOrDefault(item => item.Type == type)
        ?? throw new IntegrationException("AUTH_NOT_SUPPORTED", "No authentication handler is registered for this type.");
}

public sealed class NoAuthenticationHandler : IIntegrationAuthenticationHandler
{
    public IntegrationAuthenticationType Type => IntegrationAuthenticationType.NONE;
    public Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class BasicAuthenticationHandler : IIntegrationAuthenticationHandler
{
    public IntegrationAuthenticationType Type => IntegrationAuthenticationType.BASIC;
    public Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draft.Username) || string.IsNullOrWhiteSpace(draft.Password))
            throw new IntegrationException("INVALID_CONFIGURATION", "Username and password are required for Basic authentication.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{draft.Username}:{draft.Password}")));
        return Task.CompletedTask;
    }
}

public sealed class ApiKeyAuthenticationHandler : IIntegrationAuthenticationHandler
{
    public IntegrationAuthenticationType Type => IntegrationAuthenticationType.API_KEY;
    public Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken)
    {
        var value = draft.ApiKey ?? draft.BearerToken;
        if (string.IsNullOrWhiteSpace(value))
            throw new IntegrationException("INVALID_CONFIGURATION", "API key is required.");
        var name = draft.Designer.ApiKeyHeader ?? "APIKey";
        if (string.Equals(draft.Designer.ApiKeyPlacement, "QUERY", StringComparison.OrdinalIgnoreCase))
        {
            var uri = request.RequestUri ?? throw new IntegrationException("INVALID_CONFIGURATION", "A request URL is required.");
            var builder = new UriBuilder(uri);
            var query = string.IsNullOrWhiteSpace(builder.Query) ? "" : builder.Query.TrimStart('?') + "&";
            builder.Query = $"{query}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}";
            request.RequestUri = builder.Uri;
        }
        else
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }
        return Task.CompletedTask;
    }
}

public sealed class StaticBearerAuthenticationHandler : IIntegrationAuthenticationHandler
{
    public IntegrationAuthenticationType Type => IntegrationAuthenticationType.BEARER_TOKEN;
    public Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draft.BearerToken))
            throw new IntegrationException("INVALID_CONFIGURATION", "Bearer token is required.");
        var header = draft.Designer.AuthorizationHeaderName ?? "Authorization";
        if (header.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", draft.BearerToken);
        else
            request.Headers.TryAddWithoutValidation(header, $"Bearer {draft.BearerToken}");
        return Task.CompletedTask;
    }
}

public sealed class CustomHeaderAuthenticationHandler : IIntegrationAuthenticationHandler
{
    public IntegrationAuthenticationType Type => IntegrationAuthenticationType.CUSTOM_HEADER;
    public Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken)
    {
        var value = draft.ApiKey ?? draft.BearerToken;
        if (string.IsNullOrWhiteSpace(value))
            throw new IntegrationException("INVALID_CONFIGURATION", "Custom header value is required.");
        request.Headers.TryAddWithoutValidation(draft.Designer.CustomHeaderName ?? "X-API-Key", value);
        return Task.CompletedTask;
    }
}

public abstract class TokenAuthenticationHandlerBase : IIntegrationAuthenticationHandler
{
    public abstract IntegrationAuthenticationType Type { get; }

    public async Task AuthenticateAsync(HttpClient client, HttpRequestMessage request, IntegrationDesignerDraft draft, IntegrationAuthContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(context.AccessToken))
        {
            var acquired = await AcquireTokenAsync(client, draft, cancellationToken);
            context.AccessToken = acquired.Token;
            context.TokenType = acquired.Type;
        }
        var header = draft.Designer.AuthorizationHeaderName ?? "Authorization";
        var scheme = string.IsNullOrWhiteSpace(context.TokenType) ? "Bearer" : context.TokenType;
        if (header.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, context.AccessToken);
        else
            request.Headers.TryAddWithoutValidation(header, $"{scheme} {context.AccessToken}");
    }

    protected abstract Task<(string Token, string Type)> AcquireTokenAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken);

    protected static async Task<(string Token, string Type)> ReadTokenAsync(HttpResponseMessage response, string path, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw new IntegrationException("TOKEN_ENDPOINT_FAILED", "The token endpoint did not return a successful response.", (int)response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var token = ReadPath(json.RootElement, path);
        if (string.IsNullOrWhiteSpace(token))
            throw new IntegrationException("TOKEN_EXTRACTION_FAILED", "The token response did not contain an access token.");
        var type = ReadPath(json.RootElement, "token_type") ?? "Bearer";
        return (token, type);
    }

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

public sealed class OAuth2ClientCredentialsHandler : TokenAuthenticationHandlerBase
{
    public override IntegrationAuthenticationType Type => IntegrationAuthenticationType.OAUTH2_CLIENT_CREDENTIALS;

    protected override async Task<(string Token, string Type)> AcquireTokenAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draft.TokenEndpoint) || string.IsNullOrWhiteSpace(draft.ClientId) || string.IsNullOrWhiteSpace(draft.ClientSecret))
            throw new IntegrationException("INVALID_CONFIGURATION", "Token URL, client ID, and client secret are required for OAuth2.");
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, draft.TokenEndpoint);
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = draft.ClientId!,
            ["client_secret"] = draft.ClientSecret!,
        };
        if (!string.IsNullOrWhiteSpace(draft.TokenScope)) body["scope"] = draft.TokenScope!;
        tokenRequest.Content = new FormUrlEncodedContent(body);
        using var response = await client.SendAsync(tokenRequest, cancellationToken);
        return await ReadTokenAsync(response, draft.Designer.TokenResponsePath ?? "access_token", cancellationToken);
    }
}

public sealed class TokenApiAuthenticationHandler : TokenAuthenticationHandlerBase
{
    public override IntegrationAuthenticationType Type => IntegrationAuthenticationType.CUSTOM_TOKEN_ENDPOINT;

    protected override async Task<(string Token, string Type)> AcquireTokenAsync(HttpClient client, IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(draft.TokenEndpoint))
            throw new IntegrationException("INVALID_CONFIGURATION", "Token URL is required for Token API authentication.");
        var method = (draft.Designer.TokenHttpMethod ?? "POST").ToUpperInvariant() == "GET" ? HttpMethod.Get : HttpMethod.Post;
        var tokenUri = draft.TokenEndpoint!;
        if (method == HttpMethod.Get && draft.TokenBody is { Count: > 0 })
        {
            var query = string.Join('&', draft.TokenBody.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
            tokenUri += tokenUri.Contains('?', StringComparison.Ordinal) ? "&" + query : "?" + query;
        }
        using var tokenRequest = new HttpRequestMessage(method, tokenUri);
        foreach (var header in draft.TokenHeaders ?? [])
            tokenRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (!string.IsNullOrWhiteSpace(draft.ApiKey) && !(draft.TokenHeaders ?? []).Keys.Any(item => item.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || item.Equals("APIKey", StringComparison.OrdinalIgnoreCase)))
            tokenRequest.Headers.TryAddWithoutValidation("APIKey", draft.ApiKey);
        if (method != HttpMethod.Get)
            tokenRequest.Content = new FormUrlEncodedContent(draft.TokenBody ?? new Dictionary<string, string>());
        using var response = await client.SendAsync(tokenRequest, cancellationToken);
        return await ReadTokenAsync(response, draft.Designer.TokenResponsePath ?? "access_token", cancellationToken);
    }
}
