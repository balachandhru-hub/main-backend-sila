using System.Text.Json;
using Operations.Domain.Common;
using Operations.Domain.Entities;

namespace Operations.Application.Services.Ocr
{
    /// <summary>
    /// Sends the document to the external extraction endpoint configured for the organization.
    /// </summary>
    public class ExternalOcrProvider : IOcrProvider
    {
        private readonly ExtractionAgentConfig _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalOcrProvider(ExtractionAgentConfig configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public string Name => _configuration.ProviderType;

        public bool Unavailable => false;

        public async Task<(string? Text, decimal? Confidence)> ExtractAsync(Document document, byte[] content, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_configuration.EndpointUrl))
            {
                throw new InvalidOperationException("The external extraction endpoint is not configured.");
            }

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _configuration.EndpointUrl);
            request.Headers.Add("X-SILA-Document-Type", document.DocumentType.ToString());
            request.Content = new ByteArrayContent(content);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(document.ContentType);
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_EXTRACTION);
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"External extraction returned HTTP {(int)response.StatusCode}.");
            }

            string payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (payload.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                using JsonDocument json = JsonDocument.Parse(payload);
                string? text = json.RootElement.TryGetProperty("text", out JsonElement textElement) ? textElement.GetString() : payload;
                decimal confidence = json.RootElement.TryGetProperty("confidence", out JsonElement confidenceElement) && confidenceElement.TryGetDecimal(out decimal parsed)
                    ? parsed
                    : 0.80m;
                return (text, confidence);
            }

            return (payload, 0.80m);
        }
    }
}
