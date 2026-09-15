using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.ExceptionHandler;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;


namespace SharedKernel.Attributes
{
    /// <summary>
    /// Validates JWT token from cookie and checks permission.
    /// </summary>
    public class ApiAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        /// <summary>
        /// Permission required to access the API.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Validate the token and populate the principal, but skip the
        /// permission check. Set on the endpoint that serves the permission
        /// list itself, which cannot require a permission to be read without
        /// depending on itself.
        /// </summary>
        public bool TokenOnly { get; set; }

        /// <summary>
        /// How long a role's permissions are cached before being reloaded.
        /// </summary>
        public static readonly TimeSpan PermissionCacheLifetime = TimeSpan.FromHours(1);

        /// <summary>
        /// Cache key for a role's permissions. Login removes this key so a
        /// permission change takes effect on the next login.
        /// </summary>
        public static string PermissionCacheKey(Guid roleId) => $"permissions:role:{roleId}";

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!TokenOnly && string.IsNullOrWhiteSpace(Name))
            {
                throw new ForBiddenCustomException(
                    "Not authorized",
                    "Permission is missing.");
            }

            // Read Access Token from Cookie
            if (!context.HttpContext.Request.Cookies.TryGetValue(
                    "access_token",
                    out string? token) ||
                string.IsNullOrWhiteSpace(token))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Access token not found.");
            }

            IConfiguration configuration =
                context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

            string jwtKey = configuration["Tokens:Key"]!;
            string issuer = configuration["Tokens:Issuer"]!;

            var tokenHandler = new JwtSecurityTokenHandler();

            ClaimsPrincipal principal;

            try
            {
                principal = tokenHandler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,

                        ValidateAudience = false,

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ClockSkew = TimeSpan.Zero
                    },
                    out SecurityToken validatedToken);

                // Optional - Read JWT Expiry (UTC)
                var jwtToken = (JwtSecurityToken)validatedToken;
                DateTime issuedOnUtc = jwtToken.ValidFrom;
                DateTime expiresOnUtc = jwtToken.ValidTo;
            }
            catch (SecurityTokenExpiredException)
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Token has expired.");
            }
            catch (Exception)
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Invalid token.");
            }

            // Store authenticated user
            context.HttpContext.User = principal;

            // Read Claims
            string? userId = principal.FindFirst("UserId")?.Value;
            string? personId = principal.FindFirst("PersonId")?.Value;
            string? organizationId = principal.FindFirst("OrganizationId")?.Value;
            string? roleId = principal.FindFirst(ClaimTypes.Role)?.Value;
      

            if (TokenOnly)
            {
                return;
            }

            if (!Guid.TryParse(roleId, out Guid parsedRoleId))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Invalid token.");
            }

            // Read permissions from the role cache rather than from the token.
            // Carrying them as a claim made the access_token cookie exceed the
            // 4096-byte limit browsers enforce, and the browser then dropped
            // the cookie outright.
            List<string> permissions = await GetPermissionsAsync(
                context,
                token,
                parsedRoleId);

            // Validate Permission
            if (!permissions.Contains(Name, StringComparer.OrdinalIgnoreCase))
            {
                throw new ForBiddenCustomException(
                    "Forbidden",
                    "You don't have permission to access this API.");
            }
        }

        /// <summary>
        /// Returns the role's permissions from the in-memory cache, loading them
        /// from Identity on a miss.
        /// </summary>
        /// <remarks>
        /// Every service, Identity included, loads them over HTTP: this
        /// attribute lives in SharedKernel and cannot reach Identity's
        /// database. The cache is per process, so a login only clears it in
        /// Identity; other services pick up changes when their entry expires.
        /// </remarks>
        private static async Task<List<string>> GetPermissionsAsync(
            AuthorizationFilterContext context,
            string token,
            Guid roleId)
        {
            IServiceProvider services = context.HttpContext.RequestServices;
            IMemoryCache cache = services.GetRequiredService<IMemoryCache>();
            string key = PermissionCacheKey(roleId);

            if (cache.TryGetValue(key, out List<string>? cached) && cached is not null)
            {
                return cached;
            }

            IConfiguration configuration = services.GetRequiredService<IConfiguration>();
            string? identityUrl = configuration["InterCallService:IdentityUrl"];

            if (string.IsNullOrWhiteSpace(identityUrl))
            {
                throw new InvalidOperationException(
                    "'InterCallService:IdentityUrl' is not configured.");
            }

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{identityUrl.TrimEnd('/')}/api/v1/identity/auth/permissions");

            // Forward the caller's own token, so Identity resolves the role
            // from it and a caller can only read its own permissions.
            request.Headers.Add("Cookie", $"access_token={token}");

            HttpClient httpClient = services
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient();

            HttpResponseMessage response = await httpClient.SendAsync(
                request,
                context.HttpContext.RequestAborted);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Invalid token.");
            }

            if (!response.IsSuccessStatusCode)
            {
                // Don't cache or authorize on an empty list when Identity is
                // down - that would turn an outage into misleading 403s.
                throw new BadRequestCustomException(
                    "Unable to resolve permissions.",
                    await response.Content.ReadAsStringAsync());
            }

            List<string> permissions =
                await response.Content.ReadFromJsonAsync<List<string>>()
                ?? new List<string>();

            cache.Set(key, permissions, PermissionCacheLifetime);

            return permissions;
        }
    }
}
