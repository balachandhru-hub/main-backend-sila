using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.ExceptionHandler;

namespace SharedKernel.Security
{
    /// <summary>
    /// Validates a JWT issued by the Identity service. Shared by SignalR hubs (Buyer.API's
    /// MessageHub, Supplier.API's NotificationHub) which can't rely on the normal
    /// [Authorize]/JwtBearer middleware pipeline because hub methods bypass it - the token
    /// has to be pulled off the connection (cookie or querystring) and validated by hand.
    /// </summary>
    public static class AccessTokenValidator
    {
        public const string CookieName = "access_token";
        public const string QueryParameterName = "access_token";

        public static ClaimsPrincipal Validate(string? token, string jwtKey, string issuer)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnAuthorizedCustomException("Unauthorized", "Access token not found.");
            }

            JwtSecurityTokenHandler tokenHandler = new();

            try
            {
                ClaimsPrincipal principal = tokenHandler.ValidateToken(
                    token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                        ClockSkew = TimeSpan.Zero
                    },
                    out _);

                return principal;
            }
            catch (Exception)
            {
                throw new UnAuthorizedCustomException("Unauthorized", "Invalid or expired token.");
            }
        }
    }
}
