
using SharedKernel.ExceptionHandler;
using Microsoft.Extensions.Configuration;

using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.LoggerServices;
using Buyer.Infrastructure.Contracts.IServices;


namespace Services
{
    /// <summary>
    /// Service class for UserIdentity Service
    /// </summary>
    public class UserIdentityService : IUserIdentityService
    {
        private readonly IConfiguration _configuration;

        private readonly ILoggerManager _logger;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Contructor used for injecting dependencies.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor for accessing the current HTTP context.</param>
        /// <param name="configuration">The configuration for accessing application settings.</param>
        /// <param name = "logger" > The logger for logging messages.</param>
        public UserIdentityService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILoggerManager logger, IUserContext userContext)
        {
            _configuration = configuration;
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
            _logger.LogInfo("Fetching current user");
            Guid userId;
            if (_httpContextAccessor.HttpContext != null)
            {
                ClaimsPrincipal user = _httpContextAccessor.HttpContext.User;
                if (user.Identity != null && user.Identity.IsAuthenticated)
                {
                    Claim? userIdClaim = user.FindFirst("UserId");
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out userId))
                    {
                        _logger.LogInfo($"Current user ID retrieved successfully: {userId}");
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