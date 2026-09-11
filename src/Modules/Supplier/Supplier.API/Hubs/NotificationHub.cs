using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.API.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly IConfiguration _configuration;
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public NotificationHub(IConfiguration configuration, IRepositoryWrapper repository, ILoggerManager logger)
        {
            _configuration = configuration;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Joins the caller to a supplier-level RFQ group-conversation. The conversation itself
        /// is owned by the Buyer service, so participancy is checked here against this
        /// service's own RFQOrganizationUserMapping mirror - the caller's organization must have
        /// an active mapping for this exact (RFQId, SupplierId) pair. Checking SupplierId here
        /// (not just RFQId) is required: several suppliers can be invited to the same RFQ, and
        /// without this check a caller could join another supplier's group by guessing its
        /// SupplierId while both suppliers are invited to the same RFQ.
        /// </summary>
        public async Task JoinConversation(Guid rfqId, Guid supplierId)
        {
            ClaimsPrincipal principal = ValidateAccessTokenCookie();

            string? organizationIdClaim = principal.FindFirst("OrganizationId")?.Value;

            if (!Guid.TryParse(organizationIdClaim, out Guid organizationId))
            {
                throw new HubException("Unauthorized.");
            }

            var orgMapping = await _repository.RFQOrganizationUserMapping
                .FindFirstByConditionAsync(x =>
                    x.BuyerRFQId == rfqId &&
                    x.OrganizationId == organizationId &&
                    x.SupplierId == supplierId &&
                    x.IsActive);

            if (orgMapping == null)
            {
                _logger.LogError($"Forbidden JoinConversation attempt. RFQId: {rfqId}, SupplierId: {supplierId}, OrganizationId: {organizationId}");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(rfqId, supplierId));
        }

        public static string GroupName(Guid rfqId, Guid supplierId) => $"rfq:{rfqId}:supplier:{supplierId}";

        private ClaimsPrincipal ValidateAccessTokenCookie()
        {
            string? token = Context.GetHttpContext()?.Request.Cookies["access_token"];

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new HubException("Access token not found.");
            }

            string jwtKey = _configuration["Tokens:Key"]!;
            string issuer = _configuration["Tokens:Issuer"]!;

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
                throw new HubException("Invalid or expired token.");
            }
        }
    }
}