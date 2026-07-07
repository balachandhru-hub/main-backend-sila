using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Contracts.IServices;
using Identity.Domain.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ILoggerManager _logger;

        public AuthService(ILoggerManager logger)
        {
            _logger = logger;
        }

        public TokenClaimDto GetClaim(string token)
        {
            _logger.LogInfo("Fetching Claim Details");

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Access token not found.");
            }

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(token);

            Dictionary<string, string> claims =
                jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            TokenClaimDto dto = new TokenClaimDto
            {
                UserId = Guid.Parse(claims["UserId"]),
                RoleId = Guid.Parse(claims["role"]),
                PersonId = Guid.Parse(claims["PersonId"]),
                OrganizationId = Guid.Parse(claims["OrganizationId"])
            };

            if (claims.TryGetValue("Permissions", out var permissions))
            {
                dto.Permissions =
                    JsonSerializer.Deserialize<List<string>>(permissions)
                    ?? new List<string>();
            }

            _logger.LogInfo("Fetched Claim Details");

            return dto;
        }
    }
}