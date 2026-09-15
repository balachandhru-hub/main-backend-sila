using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using SharedKernel.Security;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.API.Hubs
{
    /// <summary>
    /// Real-time hub for RFQ message threads on the Supplier side. Every caller here belongs
    /// to a Supplier organization (Buyer users connect to Buyer.API's MessageHub instead) - the
    /// conversation itself is owned by the Buyer service, so this hub only relays events the
    /// Buyer service pushes to it via the internal notify endpoint.
    ///
    /// On connect, the caller is automatically joined to every RFQ+Supplier conversation group
    /// their organization is a party to, not just the one thread currently open in the UI, so a
    /// logged-in user still receives NewMessage/NewMessageNotification for a conversation while
    /// they're not looking at that specific chat and the frontend can raise an unread badge.
    /// </summary>
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

        public override async Task OnConnectedAsync()
        {
            ClaimsPrincipal? principal;

            try
            {
                principal = ValidateAccessToken();
            }
            catch (Exception ex)
            {
                _logger.LogError($"NotificationHub connection rejected. ConnectionId: {Context.ConnectionId}. {ex.Message}");
                Context.Abort();
                return;
            }

            string? organizationIdClaim = principal.FindFirst("OrganizationId")?.Value;

            if (!Guid.TryParse(organizationIdClaim, out Guid organizationId))
            {
                _logger.LogError($"NotificationHub connection rejected - missing organization claim. ConnectionId: {Context.ConnectionId}.");
                Context.Abort();
                return;
            }

            try
            {
                List<string> groups = _repository.RFQOrganizationUserMapping
                    .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                    .Select(x => GroupName(x.BuyerRFQId, x.SupplierId))
                    .Distinct()
                    .ToList();

                foreach (string groupName in groups)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to auto-join conversations on connect. OrganizationId: {organizationId}. {ex}");
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogError($"NotificationHub connection {Context.ConnectionId} closed with error: {exception}");
            }

            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Joins the caller to a supplier-level RFQ group-conversation. Checking SupplierId here
        /// (not just RFQId) is required: several suppliers can be invited to the same RFQ, and
        /// without this check a caller could join another supplier's group by guessing its
        /// SupplierId while both suppliers are invited to the same RFQ.
        /// </summary>
        public async Task JoinConversation(Guid rfqId, Guid supplierId)
        {
            ClaimsPrincipal principal = ValidateAccessToken();

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

            _logger.LogInfo($"ConnectionId {Context.ConnectionId} joined {GroupName(rfqId, supplierId)}.");

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(rfqId, supplierId));
        }

        /// <summary>Alias matching the frontend's JoinChat naming.</summary>
        public Task JoinChat(Guid rfqId, Guid supplierId) => JoinConversation(rfqId, supplierId);

        public Task LeaveConversation(Guid rfqId, Guid supplierId)
        {
            _logger.LogInfo($"ConnectionId {Context.ConnectionId} left {GroupName(rfqId, supplierId)}.");

            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(rfqId, supplierId));
        }

        /// <summary>Alias matching the frontend's LeaveChat naming.</summary>
        public Task LeaveChat(Guid rfqId, Guid supplierId) => LeaveConversation(rfqId, supplierId);

        public static string GroupName(Guid rfqId, Guid supplierId) => $"rfq:{rfqId}:supplier:{supplierId}";

        private ClaimsPrincipal ValidateAccessToken()
        {
            HttpContext? httpContext = Context.GetHttpContext();

            string? token = httpContext?.Request.Cookies[AccessTokenValidator.CookieName];

            if (string.IsNullOrWhiteSpace(token))
            {
                token = httpContext?.Request.Query[AccessTokenValidator.QueryParameterName];
            }

            string jwtKey = _configuration["Tokens:Key"]!;
            string issuer = _configuration["Tokens:Issuer"]!;

            try
            {
                return AccessTokenValidator.Validate(token, jwtKey, issuer);
            }
            catch (UnAuthorizedCustomException ex)
            {
                throw new HubException(ex.Message);
            }
        }
    }
}
