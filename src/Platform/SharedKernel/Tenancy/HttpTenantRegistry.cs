using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// Realm registry of every service except Identity: a copy fetched from Identity's internal endpoint.
    /// The response carries no secrets; each service derives its own connection string from the realm code.
    /// </summary>
    public class HttpTenantRegistry : TenantRegistryBase
    {
        public const string INTERNAL_KEY_HEADER = "X-Internal-Key";
        public const string REGISTRY_PATH = "api/v1/identity/internal/tenants";

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpTenantRegistry(IConfiguration configuration, IHttpClientFactory httpClientFactory)
            : base(configuration)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task<List<TenantInfo>> LoadAsync(CancellationToken cancellationToken)
        {
            string? identityUrl = _configuration["InterCallService:IdentityUrl"];
            if (string.IsNullOrWhiteSpace(identityUrl))
            {
                throw new InvalidOperationException("'InterCallService:IdentityUrl' is not configured.");
            }

            HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{identityUrl.TrimEnd('/')}/{REGISTRY_PATH}");
            request.Headers.Add(INTERNAL_KEY_HEADER, InternalKey(_configuration));

            HttpClient httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            List<TenantInfo>? tenants = await response.Content.ReadFromJsonAsync<List<TenantInfo>>(cancellationToken);
            return tenants ?? new List<TenantInfo>();
        }

        /// <summary>
        /// Key for service-to-service calls, derived from the token signing key every service already shares.
        /// It is a one-way hash, so presenting it does not reveal the signing key.
        /// </summary>
        public static string InternalKey(IConfiguration configuration)
        {
            string signingKey = configuration["Tokens:Key"]
                ?? throw new InvalidOperationException("'Tokens:Key' is not configured.");
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{signingKey}:internal-service-call"));
            return Convert.ToHexString(hash);
        }
    }
}
