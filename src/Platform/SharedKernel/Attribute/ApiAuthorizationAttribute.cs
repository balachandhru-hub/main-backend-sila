using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.ExceptionHandler;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


namespace SharedKernel.Attributes
{
    /// <summary>
    /// Validates JWT token from cookie and checks permission.
    /// </summary>
    public class ApiAuthorizationAttribute : Attribute, IAuthorizationFilter
    {
        /// <summary>
        /// Permission required to access the API.
        /// </summary>
        public string? Name { get; set; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (string.IsNullOrWhiteSpace(Name))
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
      

            // Read Permission Claims
           string? permissionJson = principal.FindFirst("Permissions")?.Value;

List<string> permissions = string.IsNullOrWhiteSpace(permissionJson)
    ? new List<string>()
    : JsonSerializer.Deserialize<List<string>>(permissionJson)!;

            // Validate Permission
            if (!permissions.Contains(Name, StringComparer.OrdinalIgnoreCase))
            {
                throw new ForBiddenCustomException(
                    "Forbidden",
                    "You don't have permission to access this API.");
            }
        }
    }
}