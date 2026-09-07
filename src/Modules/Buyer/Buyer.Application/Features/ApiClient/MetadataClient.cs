using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

public class MetadataApiClient : IMetadataApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILoggerManager _logger;

    public MetadataApiClient(HttpClient httpClient, IConfiguration configuration, ILoggerManager logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<MetadataDto>?> GetReferenceList(List<string> key)
    {
        string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;
        var response = await _httpClient.PostAsync(
            $"{masterDataUrl}/api/v1/masterdata/metadata/reference-list",
            new StringContent(JsonSerializer.Serialize(key), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<MetadataDto>?>();
    }
    public async Task<string> GetRefTermKeyById(Guid id)
    {
        string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

        var response = await _httpClient.GetAsync(
            $"{masterDataUrl}/api/v1/masterdata/metadata/{id}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task SendEmailAsync(
        string toEmail,
        string emailKey,
        Guid? entityId,
        string? entityType,
        Dictionary<string, string>? parameters,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Sending email. EmailKey: {emailKey}, ToEmail: {toEmail}");

        string masterDataUrl = _configuration[Common.MASTER_DATA_URL]!;

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{masterDataUrl}/api/v1/masterdata/email/send");

        request.Content = JsonContent.Create(new
        {
            ToEmail = toEmail,
            EmailKey = emailKey,
            EntityId = entityId,
            EntityType = entityType,
            Parameters = parameters
        });

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError($"Failed to send email. EmailKey: {emailKey}, Status Code: {response.StatusCode}");
            throw new FailedDependencyCustomException(
                "Failed to send email.",
                $"EmailKey: {emailKey}, StatusCode: {response.StatusCode}, Response: {error}");
        }

        _logger.LogInfo($"Email sent successfully. EmailKey: {emailKey}, ToEmail: {toEmail}");
    }
}