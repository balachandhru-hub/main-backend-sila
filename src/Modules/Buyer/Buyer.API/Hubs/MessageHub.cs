using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.API.Hubs
{
    /// <summary>
    /// Real-time hub for RFQ message threads. Hub methods don't go through the
    /// [ApiAuthorization] MVC filter, so the access_token cookie is validated
    /// manually here before a caller is allowed to join a thread's group.
    /// </summary>
    public class MessageHub : Hub
    {
        private readonly IConfiguration _configuration;
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public MessageHub(IConfiguration configuration, IRepositoryWrapper repository, ILoggerManager logger)
        {
            _configuration = configuration;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Joins the caller to a supplier-level RFQ group-conversation. The group is keyed by
        /// (RFQId, SupplierId) - never by UserId or ThreadId - so it can be joined even before
        /// any MessageThread row exists yet (e.g. a supplier's first user registering before
        /// the buyer has sent a message), and every user of that supplier shares the same group.
        /// </summary>
        public Task JoinConversation(Guid rfqId, Guid supplierId)
        {
            ClaimsPrincipal principal = ValidateAccessTokenCookie();

            string? organizationIdClaim = principal.FindFirst("OrganizationId")?.Value;
            string? organizationType = principal.FindFirst("OrganizationType")?.Value;

            if (!Guid.TryParse(organizationIdClaim, out Guid organizationId) || string.IsNullOrWhiteSpace(organizationType))
            {
                throw new HubException("Unauthorized.");
            }

            bool isBuyer = string.Equals(organizationType, "Buyer", StringComparison.OrdinalIgnoreCase);
            bool isSupplier = string.Equals(organizationType, "Supplier", StringComparison.OrdinalIgnoreCase);

            bool authorized;

            if (isBuyer)
            {
                BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

                bool ownsRfq = buyer != null && _repository.RFQ
                    .FindFirstByCondition(x => x.Id == rfqId && x.BuyerId == buyer.Id && x.IsActive) != null;

                bool supplierInvited = ownsRfq && _repository.RFQSupplierMapping
                    .FindFirstByCondition(x => x.RFQId == rfqId && x.SupplierId == supplierId && x.IsActive) != null;

                authorized = supplierInvited;
            }
            else if (isSupplier)
            {
                RFQOrganizationUserMapping? orgMapping = _repository.RFQOrganizationUserMapping
                    .FindFirstByCondition(x =>
                        x.RFQId == rfqId &&
                        x.OrganizationId == organizationId &&
                        x.SupplierId == supplierId &&
                        x.IsActive);

                authorized = orgMapping != null;
            }
            else
            {
                authorized = false;
            }

            if (!authorized)
            {
                _logger.LogError($"Forbidden JoinConversation attempt. RFQId: {rfqId}, SupplierId: {supplierId}, OrganizationId: {organizationId}, OrganizationType: {organizationType}");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
            }

            return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(rfqId, supplierId));
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
