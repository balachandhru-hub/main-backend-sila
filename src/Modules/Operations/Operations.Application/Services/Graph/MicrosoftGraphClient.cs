using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;

namespace Operations.Application.Services.Graph
{
    public interface IMicrosoftGraphClient
    {
        MicrosoftReadinessResponseDto GetReadiness();
        string GetTenantId();
        string BuildAuthorizationUrl(string state);
        Task<MicrosoftToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
        Task<MicrosoftToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        string ProtectToken(MicrosoftToken token);
        MicrosoftToken UnprotectToken(string? protectedValue);
        Task<GraphSite> ResolveSiteAsync(string accessToken, string siteUrl, CancellationToken cancellationToken);
        Task<List<GraphDrive>> ListDrivesAsync(string accessToken, string siteId, CancellationToken cancellationToken);
        Task<GraphDrive> ResolveDriveAsync(string accessToken, string siteId, string? driveId, string? driveName, CancellationToken cancellationToken);
        Task<GraphFolder> ResolveFolderAsync(string accessToken, string driveId, string? folderId, string? folderPath, CancellationToken cancellationToken);
        Task<List<GraphFolder>> ListChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken cancellationToken);
        Task TestWriteAsync(string accessToken, string driveId, string folderId, CancellationToken cancellationToken);
        Task<GraphDriveItem> UploadFileAsync(string accessToken, string driveId, string folderId, string fileName, byte[] content, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Microsoft identity platform + Graph (SharePoint) client. It only talks to Microsoft and
    /// encrypts / decrypts the token; connections and destinations are handled by the handlers.
    /// Configuration keys are SILAME's: Microsoft:TenantId, ClientId, ClientSecret, RedirectUri,
    /// TokenEncryptionKey and the optional Microsoft:Scopes.
    /// </summary>
    public class MicrosoftGraphClient : IMicrosoftGraphClient
    {
        private const string GraphBase = "https://graph.microsoft.com/v1.0";
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MicrosoftGraphClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public MicrosoftReadinessResponseDto GetReadiness()
        {
            bool tenant = !string.IsNullOrWhiteSpace(Get("TenantId"));
            bool clientId = !string.IsNullOrWhiteSpace(Get("ClientId"));
            bool clientSecret = !string.IsNullOrWhiteSpace(Get("ClientSecret"));
            bool redirectUri = !string.IsNullOrWhiteSpace(Get("RedirectUri"));
            bool encryptionKey = !string.IsNullOrWhiteSpace(Get("TokenEncryptionKey"));
            return new MicrosoftReadinessResponseDto
            {
                TenantConfigured = tenant,
                ClientIdConfigured = clientId,
                ClientSecretConfigured = clientSecret,
                RedirectUriConfigured = redirectUri,
                TokenEncryptionConfigured = encryptionKey,
                GraphIntegrationReady = tenant && clientId && clientSecret && redirectUri && encryptionKey,
                RedirectUri = Get("RedirectUri")
            };
        }

        public string GetTenantId()
        {
            return GetRequired("TenantId");
        }

        public string BuildAuthorizationUrl(string state)
        {
            EnsureConfigured();
            return $"https://login.microsoftonline.com/{Uri.EscapeDataString(GetRequired("TenantId"))}/oauth2/v2.0/authorize"
                + $"?client_id={Uri.EscapeDataString(GetRequired("ClientId"))}"
                + "&response_type=code"
                + $"&redirect_uri={Uri.EscapeDataString(GetRequired("RedirectUri"))}"
                + $"&response_mode=query&scope={Uri.EscapeDataString(Scopes())}"
                + $"&state={Uri.EscapeDataString(state)}&prompt=select_account";
        }

        public Task<MicrosoftToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            EnsureConfigured();
            return RequestTokenAsync(new Dictionary<string, string>
            {
                ["client_id"] = GetRequired("ClientId"),
                ["client_secret"] = GetRequired("ClientSecret"),
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = GetRequired("RedirectUri"),
                ["scope"] = Scopes(),
            }, null, cancellationToken);
        }

        public Task<MicrosoftToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            EnsureConfigured();
            return RequestTokenAsync(new Dictionary<string, string>
            {
                ["client_id"] = GetRequired("ClientId"),
                ["client_secret"] = GetRequired("ClientSecret"),
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["scope"] = Scopes(),
            }, refreshToken, cancellationToken);
        }

        public string ProtectToken(MicrosoftToken token)
        {
            byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(GetRequired("TokenEncryptionKey")));
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plaintext = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(token));
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            using AesGcm aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
            return $"v1:{Base64Url(nonce)}:{Base64Url(tag)}:{Base64Url(ciphertext)}";
        }

        public MicrosoftToken UnprotectToken(string? protectedValue)
        {
            if (string.IsNullOrWhiteSpace(protectedValue) || !protectedValue.StartsWith("v1:", StringComparison.Ordinal))
            {
                throw new MicrosoftGraphException("CREDENTIAL_NOT_FOUND", "Microsoft authorization must be completed again.");
            }

            string[] parts = protectedValue.Split(':');
            if (parts.Length != 4)
            {
                throw new MicrosoftGraphException("CREDENTIAL_INVALID", "Microsoft authorization data is invalid.");
            }

            try
            {
                byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes(GetRequired("TokenEncryptionKey")));
                byte[] ciphertext = FromBase64Url(parts[3]);
                byte[] plaintext = new byte[ciphertext.Length];
                using AesGcm aes = new AesGcm(key, 16);
                aes.Decrypt(FromBase64Url(parts[1]), ciphertext, FromBase64Url(parts[2]), plaintext);
                return JsonSerializer.Deserialize<MicrosoftToken>(Encoding.UTF8.GetString(plaintext))
                    ?? throw new MicrosoftGraphException("CREDENTIAL_NOT_FOUND", "Microsoft authorization must be completed again.");
            }
            catch (Exception exception) when (exception is CryptographicException or FormatException or JsonException)
            {
                throw new MicrosoftGraphException("CREDENTIAL_INVALID", "Microsoft authorization data is invalid.");
            }
        }

        public Task<GraphSite> ResolveSiteAsync(string accessToken, string siteUrl, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out Uri? uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase))
            {
                throw new MicrosoftGraphException("SITE_URL_INVALID", "Enter a valid HTTPS SharePoint site URL.");
            }

            string path = string.IsNullOrWhiteSpace(uri.AbsolutePath) ? "/" : uri.AbsolutePath.TrimEnd('/');
            return GetAsync<GraphSite>($"/sites/{uri.Host}:{path}", accessToken, cancellationToken);
        }

        public async Task<List<GraphDrive>> ListDrivesAsync(string accessToken, string siteId, CancellationToken cancellationToken)
        {
            GraphCollection<GraphDrive> drives = await GetAsync<GraphCollection<GraphDrive>>($"/sites/{Uri.EscapeDataString(siteId)}/drives", accessToken, cancellationToken);
            return drives.Value;
        }

        public async Task<GraphDrive> ResolveDriveAsync(string accessToken, string siteId, string? driveId, string? driveName, CancellationToken cancellationToken)
        {
            List<GraphDrive> drives = await ListDrivesAsync(accessToken, siteId, cancellationToken);
            GraphDrive? drive = !string.IsNullOrWhiteSpace(driveId)
                ? drives.SingleOrDefault(item => item.Id == driveId)
                : drives.FirstOrDefault(item => string.Equals(item.Name, driveName ?? "Documents", StringComparison.OrdinalIgnoreCase)) ?? drives.FirstOrDefault();
            return drive ?? throw new MicrosoftGraphException("LIBRARY_NOT_FOUND", "The selected SharePoint document library is not accessible.");
        }

        public Task<GraphFolder> ResolveFolderAsync(string accessToken, string driveId, string? folderId, string? folderPath, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                return GetAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folderId)}", accessToken, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return GetAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(driveId)}/root", accessToken, cancellationToken);
            }

            string path = "/" + folderPath.Trim().Trim('/');
            return GetAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(driveId)}/root:{path}:", accessToken, cancellationToken);
        }

        public async Task<List<GraphFolder>> ListChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken cancellationToken)
        {
            GraphCollection<GraphFolder> children = await GetAsync<GraphCollection<GraphFolder>>(
                $"/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(parentFolderId)}/children", accessToken, cancellationToken);
            return children.Value.Where(item => item.Folder != null).ToList();
        }

        public async Task TestWriteAsync(string accessToken, string driveId, string folderId, CancellationToken cancellationToken)
        {
            string fileName = $"operations-connection-test-{Guid.NewGuid():N}.txt";
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_GRAPH);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put,
                $"{GraphBase}/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folderId)}:/{fileName}:/content")
            {
                Content = new StringContent("SharePoint connection test", Encoding.UTF8, "text/plain"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new MicrosoftGraphException("WRITE_ACCESS_DENIED", "The application could not write to the selected SharePoint folder.");
            }

            GraphDriveItem? created = await response.Content.ReadFromJsonAsync<GraphDriveItem>(cancellationToken: cancellationToken);
            if (created?.Id == null)
            {
                throw new MicrosoftGraphException("WRITE_TEST_INVALID", "Microsoft did not confirm the validation file.");
            }

            using HttpRequestMessage delete = new HttpRequestMessage(HttpMethod.Delete,
                $"{GraphBase}/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(created.Id)}");
            delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage deleted = await client.SendAsync(delete, cancellationToken);
        }

        public async Task<GraphDriveItem> UploadFileAsync(string accessToken, string driveId, string folderId, string fileName, byte[] content, CancellationToken cancellationToken)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put,
                $"{GraphBase}/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folderId)}:/{Uri.EscapeDataString(fileName)}:/content");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new ByteArrayContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            using HttpResponseMessage response = await _httpClientFactory.CreateClient(Common.HTTP_CLIENT_GRAPH).SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new MicrosoftGraphException("TOKEN_EXPIRED", "Microsoft authorization has expired.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new MicrosoftGraphException("GRAPH_FORBIDDEN", "Microsoft denied access to the SharePoint destination.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new MicrosoftGraphException("GRAPH_UPLOAD_FAILED", "Microsoft could not store the document.");
            }

            GraphDriveItem uploaded = await response.Content.ReadFromJsonAsync<GraphDriveItem>(cancellationToken: cancellationToken)
                ?? throw new MicrosoftGraphException("GRAPH_RESPONSE_INVALID", "Microsoft returned an invalid upload response.");
            uploaded.Name ??= fileName;
            return uploaded;
        }

        private async Task<MicrosoftToken> RequestTokenAsync(Dictionary<string, string> form, string? previousRefreshToken, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_GRAPH);
            using HttpResponseMessage response = await client.PostAsync(
                $"https://login.microsoftonline.com/{Uri.EscapeDataString(GetRequired("TenantId"))}/oauth2/v2.0/token",
                new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new MicrosoftGraphException("TOKEN_EXCHANGE_FAILED", "Microsoft authorization could not be completed.");
            }

            MicrosoftTokenResponse payload = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new MicrosoftGraphException("TOKEN_RESPONSE_INVALID", "Microsoft returned an invalid authorization response.");
            return new MicrosoftToken
            {
                AccessToken = payload.AccessToken,
                RefreshToken = payload.RefreshToken ?? previousRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(payload.ExpiresIn)
            };
        }

        private async Task<T> GetAsync<T>(string path, string accessToken, CancellationToken cancellationToken)
        {
            HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_GRAPH);
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, GraphBase + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new MicrosoftGraphException("TOKEN_EXPIRED", "Microsoft authorization has expired.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new MicrosoftGraphException("GRAPH_FORBIDDEN", "Microsoft denied access to the selected SharePoint destination.");
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new MicrosoftGraphException("GRAPH_RESOURCE_NOT_FOUND", "Microsoft could not resolve the selected SharePoint resource.");
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new MicrosoftGraphException("GRAPH_RESPONSE_INVALID", "Microsoft returned an invalid SharePoint response.");
        }

        private void EnsureConfigured()
        {
            if (!GetReadiness().GraphIntegrationReady)
            {
                throw new MicrosoftGraphException("MICROSOFT_CONFIGURATION_REQUIRED", "Microsoft integration requires TenantId, ClientId, ClientSecret, RedirectUri, and TokenEncryptionKey configuration.");
            }
        }

        private string Scopes()
        {
            return Get("Scopes") ?? Common.MICROSOFT_DEFAULT_SCOPES;
        }

        private string GetRequired(string key)
        {
            return Get(key) ?? throw new MicrosoftGraphException("MICROSOFT_CONFIGURATION_REQUIRED", $"Microsoft:{key} is not configured.");
        }

        private string? Get(string key)
        {
            string? value = _configuration[$"{Common.MICROSOFT_SECTION}:{key}"];
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] FromBase64Url(string value)
        {
            return Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
        }
    }
}
