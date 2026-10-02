using System.Net.Http.Headers;
using System.Text.Json;

namespace SilaMe.Api.Services;

public static class IntegrationRequestCapture
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Json(HttpRequestMessage request, string url, int status, string body)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            {
                headers[header.Key] = "••••••••";
                continue;
            }
            headers[header.Key] = string.Join(", ", header.Value);
        }
        var safe = body.Length > 20000 ? body[..20000] : body;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            url,
            method = request.Method.Method,
            sent = true,
            headers,
            status,
            response = safe,
        }, JsonOptions);
    }

    public static void AcceptXml(HttpRequestMessage request)
    {
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/atomsvc+xml"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
    }
}
