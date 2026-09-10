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
        /// Joins the caller to a private RFQ message thread's real-time group. The thread
        /// itself is owned by the Buyer service, so participancy is checked here against
        /// this service's own RFQOrganizationUserMapping mirror (does the caller's
        /// organization have an active invite for this RFQ) - the threadId itself is only
        /// ever handed to a caller by the Buyer service's own authorized thread-list
        /// endpoint, scoped to that caller's SupplierId.
        /// </summary>
        public async Task JoinThread(Guid rfqId, Guid threadId)
        {
            ClaimsPrincipal principal = ValidateAccessTokenCookie();

            string? organizationIdClaim = principal.FindFirst("OrganizationId")?.Value;

            if (!Guid.TryParse(organizationIdClaim, out Guid organizationId))
            {
                throw new HubException("Unauthorized.");
            }

            var orgMapping = await _repository.RFQOrganizationUserMapping
                .FindFirstByConditionAsync(x => x.BuyerRFQId == rfqId && x.OrganizationId == organizationId && x.IsActive);

            if (orgMapping == null)
            {
                _logger.LogError($"Forbidden JoinThread attempt. RFQId: {rfqId}, ThreadId: {threadId}, OrganizationId: {organizationId}");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this RFQ.");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(threadId));
        }

        public static string GroupName(Guid threadId) => $"thread-{threadId}";

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