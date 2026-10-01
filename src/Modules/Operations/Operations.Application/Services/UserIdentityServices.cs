using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Operations.Infrastructure.Contracts.IServices;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services
{
    /// <summary>
    /// Service class for UserIdentity Service
    /// </summary>
    public class UserIdentityService : IUserIdentityService
    {
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Contructor used for injecting dependencies.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor for accessing the current HTTP context.</param>
        /// <param name="logger">The logger for logging messages.</param>
        /// <param name="userContext">The user set by a background worker.</param>
        public UserIdentityService(IHttpContextAccessor httpContextAccessor, ILoggerManager logger, IUserContext userContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _userContext = userContext;
        }

        /// <summary>
        /// Gets the current user's ID from the HTTP context.
        /// </summary>
        /// <returns>The current user's ID as a GUID, or Guid.Empty if the user is not authenticated or the ID cannot be parsed.</returns>
        public Guid GetCurrentUser()
        {
            Guid userId;
            if (_httpContextAccessor.HttpContext != null)
            {
                ClaimsPrincipal user = _httpContextAccessor.HttpContext.User;
                if (user.Identity != null && user.Identity.IsAuthenticated)
                {
                    Claim? userIdClaim = user.FindFirst("UserId");
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out userId))
                    {
                        _logger.LogDebug($"Current user ID retrieved successfully: {userId}");
                        return userId;
                    }
                }
            }

            userId = _userContext.GetCurrentUserId();
            if (userId != Guid.Empty)
            {
                return userId;
            }

            return Guid.Empty;
        }
    }
}
