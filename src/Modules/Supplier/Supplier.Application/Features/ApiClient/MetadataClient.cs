using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Supplier.Domain.Common;
using Supplier.Domain.Dto;
using Microsoft.Extensions.Configuration;

public class MetadataApiClient : IMetadataApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public MetadataApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
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
}
