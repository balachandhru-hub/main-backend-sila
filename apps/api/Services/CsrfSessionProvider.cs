using System.Net.Http.Headers;
using SilaMe.Api.DTOs;

namespace SilaMe.Api.Services;

public sealed record CsrfSession(string Token, bool CookiesPresent);

public sealed class CsrfSessionProvider
{
    public async Task<CsrfSession> FetchAsync(
        HttpClient client,
        IntegrationDesignerDraft draft,
        Func<HttpRequestMessage, Task> applyAuthentication,
        CancellationToken cancellationToken)
    {
        var url = IntegrationODataUrl.CsrfFetchUrl(draft);
        var method = new HttpMethod((draft.Designer.CsrfFetchMethod ?? "GET").ToUpperInvariant());
        if (method.Method is "POST" or "PUT" or "PATCH" or "DELETE")
            method = HttpMethod.Get;
        using var request = new HttpRequestMessage(method, url);
        await applyAuthentication(request);
        var csrfHeader = draft.Designer.CsrfHeaderName ?? "X-CSRF-Token";
        request.Headers.TryAddWithoutValidation(csrfHeader, draft.Designer.CsrfHeaderValue ?? "Fetch");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        if (!string.IsNullOrWhiteSpace(draft.Designer.Accept))
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(draft.Designer.Accept));
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var token = HeaderValues(response, draft.Designer.CsrfResponseHeader ?? "X-CSRF-Token")
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item) && !item.Equals("Required", StringComparison.OrdinalIgnoreCase));
        var captured = IntegrationRequestCapture.Json(request, url, (int)response.StatusCode, body);
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(token))
            throw new IntegrationException("AUTHENTICATION_FAILED", "Authentication failed while fetching a CSRF token.", 401, captured);
        if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(token))
        {
            var code = response.StatusCode is System.Net.HttpStatusCode.Forbidden ? "AUTHORIZATION_FAILED" : "CSRF_FETCH_FAILED";
            var message = response.StatusCode is System.Net.HttpStatusCode.Forbidden
                ? "The CSRF request was forbidden."
                : "The CSRF token request failed.";
            throw new IntegrationException(code, message, (int)response.StatusCode, captured);
        }
        if (string.IsNullOrWhiteSpace(token))
            throw new IntegrationException("CSRF_TOKEN_MISSING", "The CSRF token was not returned.", (int)response.StatusCode, captured);
        var cookies = HeaderValues(response, "Set-Cookie").Any();
        return new CsrfSession(token, cookies);
    }

    private static IEnumerable<string> HeaderValues(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var headers)) return headers;
        if (response.Content.Headers.TryGetValues(name, out var content)) return content;
        return [];
    }
}
