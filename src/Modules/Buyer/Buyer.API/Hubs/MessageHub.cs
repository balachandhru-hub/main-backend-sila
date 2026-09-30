using System.Security.Claims;
using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.AspNetCore.SignalR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using SharedKernel.Security;

namespace Buyer.API.Hubs
{
    /// <summary>
    /// Real-time hub for RFQ message threads. Hub methods don't go through the
    /// [ApiAuthorization] MVC filter, so the access_token is validated manually here
    /// (from the auth cookie, or the "access_token" querystring param used by the
    /// SignalR client's accessTokenFactory) before a caller is allowed to join a group.
    ///
    /// On connect, the caller is automatically joined to every RFQ+Supplier conversation
    /// group their organization is a party to - not just the one thread currently open in
    /// the UI. This is what lets a logged-in user receive NewMessage/NewMessageNotification
    /// events for a conversation even while they are not looking at that specific chat, so
    /// the frontend can raise an unread badge instead of silently missing the event.
    /// JoinConversation/JoinChat is still needed for a brand-new conversation created after
    /// the connection was already established (e.g. buyer just invited a new supplier).
    /// </summary>
    public class MessageHub : Hub
    {
        public const string ExternalSessionTokenQueryParameterName = "session_token";
        public const string ExternalRfqIdQueryParameterName = "external_rfq_id";

        private readonly IConfiguration _configuration;
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public MessageHub(
            IConfiguration configuration,
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _configuration = configuration;
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public override async Task OnConnectedAsync()
        {
            HttpContext? httpContext = Context.GetHttpContext();
            string? externalSessionToken = httpContext?.Request.Query[ExternalSessionTokenQueryParameterName];

            if (!string.IsNullOrWhiteSpace(externalSessionToken))
            {
                await ConnectAsExternalSupplier(externalSessionToken, httpContext);
                return;
            }

            ClaimsPrincipal? principal;

            try
            {
                principal = ValidateAccessToken();
            }
            catch (Exception ex)
            {
                _logger.LogError($"MessageHub connection rejected. ConnectionId: {Context.ConnectionId}. {ex.Message}");
                Context.Abort();
                return;
            }

            string? organizationIdClaim = principal.FindFirst("OrganizationId")?.Value;
            string? organizationType = principal.FindFirst("OrganizationType")?.Value;

            if (!Guid.TryParse(organizationIdClaim, out Guid organizationId) || string.IsNullOrWhiteSpace(organizationType))
            {
                _logger.LogError($"MessageHub connection rejected - missing organization claims. ConnectionId: {Context.ConnectionId}.");
                Context.Abort();
                return;
            }

            try
            {
                foreach (string groupName in GetEntitledGroups(organizationId, organizationType))
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
                _logger.LogError($"MessageHub connection {Context.ConnectionId} closed with error: {exception}");
            }

            return base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// An ExternalSupplier has no JWT, so it connects with a session token + rfqId
        /// (query params) instead of an access_token. A session token is scoped to exactly
        /// one RFQ+ExternalSupplier, so - unlike the JWT flow - only that single group is
        /// joined, not every conversation the caller participates in.
        /// </summary>
        private async Task ConnectAsExternalSupplier(string sessionToken, HttpContext? httpContext)
        {
            string? rfqIdValue = httpContext?.Request.Query[ExternalRfqIdQueryParameterName];

            if (!Guid.TryParse(rfqIdValue, out Guid rfqId))
            {
                _logger.LogError($"MessageHub connection rejected - missing rfqId. ConnectionId: {Context.ConnectionId}.");
                Context.Abort();
                return;
            }

            ExternalSupplierSessionDto? session;

            try
            {
                session = await _supplierApiClient.ValidateExternalSessionToken(sessionToken, rfqId, Context.ConnectionAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MessageHub external-supplier connection rejected. ConnectionId: {Context.ConnectionId}. {ex.Message}");
                Context.Abort();
                return;
            }

            if (session == null)
            {
                _logger.LogError($"MessageHub connection rejected - invalid external session token. ConnectionId: {Context.ConnectionId}.");
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(session.RFQId, session.ExternalSupplierId));
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Joins the caller to a supplier-level RFQ group-conversation. The group is keyed by
        /// (RFQId, SupplierId) - never by UserId or ThreadId - so it can be joined even before
        /// any MessageThread row exists yet (e.g. a supplier's first user registering before
        /// the buyer has sent a message), and every user of that supplier shares the same group.
        /// </summary>
        public Task JoinConversation(Guid rfqId, Guid buyerId, Guid supplierId)
        {
            ValidateAccessToken();

            if (!IsAuthorizedForConversation(rfqId, buyerId, supplierId))
            {
                _logger.LogError($"Forbidden JoinConversation attempt. RFQId: {rfqId}, BuyerId: {buyerId}, SupplierId: {supplierId}.");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
            }

            _logger.LogInfo($"ConnectionId {Context.ConnectionId} joined {GroupName(rfqId, supplierId)}.");

            return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(rfqId, supplierId));
        }

        /// <summary>Alias matching the frontend's JoinChat naming.</summary>
        public Task JoinChat(Guid rfqId, Guid buyerId, Guid supplierId) => JoinConversation(rfqId, buyerId, supplierId);

        public Task LeaveConversation(Guid rfqId, Guid buyerId, Guid supplierId)
        {
            _logger.LogInfo($"ConnectionId {Context.ConnectionId} left {GroupName(rfqId, supplierId)}.");

            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(rfqId, supplierId));
        }

        /// <summary>Alias matching the frontend's LeaveChat naming.</summary>
        public Task LeaveChat(Guid rfqId, Guid buyerId, Guid supplierId) => LeaveConversation(rfqId, buyerId, supplierId);

        /// <summary>
        /// Buyer-side manual join for a brand-new ExternalSupplier conversation created after
        /// the connection was already established (mirrors JoinConversation/JoinChat).
        /// </summary>
        public Task JoinExternalConversation(Guid rfqId, Guid externalSupplierId)
        {
            ValidateAccessToken();

            bool authorized = _repository.RFQ.FindFirstByCondition(x => x.Id == rfqId && x.IsActive) != null
                && _repository.RFQExternalSupplier.FindFirstByCondition(
                    x => x.RFQId == rfqId && x.ExternalSupplierId == externalSupplierId && x.IsActive) != null;

            if (!authorized)
            {
                _logger.LogError($"Forbidden JoinExternalConversation attempt. RFQId: {rfqId}, ExternalSupplierId: {externalSupplierId}.");
                throw new ForBiddenCustomException("Forbidden", "You do not have access to this conversation.");
            }

            _logger.LogInfo($"ConnectionId {Context.ConnectionId} joined {GroupName(rfqId, externalSupplierId)}.");

            return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(rfqId, externalSupplierId));
        }

        public Task LeaveExternalConversation(Guid rfqId, Guid externalSupplierId)
        {
            _logger.LogInfo($"ConnectionId {Context.ConnectionId} left {GroupName(rfqId, externalSupplierId)}.");

            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(rfqId, externalSupplierId));
        }

        /// <summary>Group key is agnostic to counterparty type - the id is either a SupplierId or an ExternalSupplierId.</summary>
        public static string GroupName(Guid rfqId, Guid counterpartyId) => $"rfq:{rfqId}:supplier:{counterpartyId}";

        /// <summary>
        /// Validates that (rfqId, buyerId, supplierId) is a real, active RFQ+Supplier
        /// conversation - not that the caller's own organization is one of its two parties.
        /// The caller only needs a valid access_token (checked by ValidateAccessToken) plus
        /// the three conversation ids the frontend already holds.
        /// </summary>
        private bool IsAuthorizedForConversation(Guid rfqId, Guid buyerId, Guid supplierId)
        {
            bool rfqBelongsToBuyer = _repository.RFQ
                .FindFirstByCondition(x => x.Id == rfqId && x.BuyerId == buyerId && x.IsActive) != null;

            return rfqBelongsToBuyer && _repository.RFQSupplierMapping
                .FindFirstByCondition(x => x.RFQId == rfqId && x.SupplierId == supplierId && x.IsActive) != null;
        }

        /// <summary>
        /// Every (RFQId, SupplierId) conversation the caller's organization currently
        /// participates in - used to auto-join all of them as soon as the connection opens.
        /// </summary>
        private IEnumerable<string> GetEntitledGroups(Guid organizationId, string organizationType)
        {
            if (string.Equals(organizationType, "Buyer", StringComparison.OrdinalIgnoreCase))
            {
                BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile
                    .FindFirstByCondition(x => x.OrganizationId == organizationId && x.IsActive);

                if (buyer == null)
                {
                    return Enumerable.Empty<string>();
                }

                List<Guid> rfqIds = _repository.RFQ
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                    .Select(x => x.Id)
                    .ToList();

                List<string> supplierGroups = _repository.RFQSupplierMapping
                    .FindByCondition(x => rfqIds.Contains(x.RFQId) && x.IsActive)
                    .Select(x => GroupName(x.RFQId, x.SupplierId))
                    .ToList();

                List<string> externalSupplierGroups = _repository.RFQExternalSupplier
                    .FindByCondition(x => rfqIds.Contains(x.RFQId) && x.IsActive)
                    .Select(x => GroupName(x.RFQId, x.ExternalSupplierId))
                    .ToList();

                return supplierGroups.Concat(externalSupplierGroups).Distinct().ToList();
            }

            if (string.Equals(organizationType, "Supplier", StringComparison.OrdinalIgnoreCase))
            {
                return _repository.RFQOrganizationUserMapping
                    .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                    .Select(x => GroupName(x.RFQId, x.SupplierId))
                    .Distinct()
                    .ToList();
            }

            return Enumerable.Empty<string>();
        }

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
