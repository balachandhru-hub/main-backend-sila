using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Auth.Queries.GetClaim
{
    public class GetClaimQueryHandler : IRequestHandler<GetClaimQuery, TokenClaimDto>
    {
        private readonly ILoggerManager _logger;

        public GetClaimQueryHandler(ILoggerManager logger)
        {
            _logger = logger;
        }

        public Task<TokenClaimDto> Handle(GetClaimQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo("Fetching Claim Details");

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Access token not found.");
            }

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(request.Token);

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

            return Task.FromResult(dto);
        }
    }
}