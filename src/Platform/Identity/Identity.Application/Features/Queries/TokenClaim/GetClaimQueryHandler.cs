using System.IdentityModel.Tokens.Jwt;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Identity.Application.Features.Queries.Permissions;
using Identity.Application.Contracts;
using Identity.Domain.Enum;

namespace Identity.Application.Features.Auth.Queries.GetClaim
{
    public class GetClaimQueryHandler : IRequestHandler<GetClaimQuery, TokenClaimDto>
    {
        private readonly ILoggerManager _logger;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IBuyerIdApiClient _buyerIdApiClient;
        private readonly ISupplierIdApiClient _supplierIdApiClient;
        private readonly IMediator _mediator;

        public GetClaimQueryHandler(ILoggerManager logger, IBuyerApiClient buyerApiClient, ISupplierApiClient supplierApiClient, IBuyerIdApiClient buyerIdApiClient, ISupplierIdApiClient supplierIdApiClient, IMediator mediator)
        {
            _buyerApiClient = buyerApiClient;
            _supplierApiClient = supplierApiClient;
            _buyerIdApiClient = buyerIdApiClient;
            _supplierIdApiClient = supplierIdApiClient;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<TokenClaimDto> Handle(GetClaimQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo("Fetching Claim Details");

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogError("Access token not found.");
                throw new UnAuthorizedCustomException("Unauthorized","Access token not found.");
            }

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(request.Token);

            Dictionary<string, string> claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            TokenClaimDto dto = new TokenClaimDto
            {
                UserId = Guid.Parse(claims["UserId"]),
                RoleId = Guid.Parse(claims["role"]),
                PersonId = Guid.Parse(claims["PersonId"]),
                OrganizationId = Guid.Parse(claims["OrganizationId"]),

            };


            // The token no longer carries the permission keys, so read them
            // from the database. The response shape is unchanged.
            dto.Permissions = await _mediator.Send(
                new GetPermissionsQuery { RoleId = dto.RoleId },
                cancellationToken);

            _logger.LogInfo($"Fetched {dto.Permissions.Count} Permissions");
            if (claims.TryGetValue("OrganizationType", out var organizationType))
            {

                _logger.LogInfo($"Fetched Organization Type: {organizationType}");
                if (Enum.TryParse<OrganizationType>(
                    organizationType,
                    true,
                    out var orgType))
                {
                    _logger.LogInfo($"Fetching Organization Id for Organization Type: {orgType}");
                    if (orgType == OrganizationType.Buyer)
                    {
                        _logger.LogInfo($"Fetching Buyer Id for Organization: {dto.OrganizationId}");
                        dto.BuyerId = await _buyerIdApiClient.GetBuyerId(request.Token);
                        dto.OrganizationType = OrganizationType.Buyer;
                    }
                    else if (orgType == OrganizationType.Supplier)
                    {
                        _logger.LogInfo($"Fetching Supplier Id for Organization: {dto.OrganizationId}");
                        dto.SupplierId = await _supplierIdApiClient.GetSupplierId(request.Token);
                        dto.OrganizationType = OrganizationType.Supplier;
                    }
                }
            }

            _logger.LogInfo("Fetched Claim Details");

            return dto;
        }
    }
}