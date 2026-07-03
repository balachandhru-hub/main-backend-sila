using System.Security.Claims;
using MasterData.Infrastructure.Contracts.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Services;

public class UserIdentityService : IUserIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContext _userContext;
    private readonly ILoggerManager _logger;

    public UserIdentityService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILoggerManager logger,
        IUserContext userContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
        _logger = logger;
    }

    public Guid GetCurrentUser()
    {
        _logger.LogInfo("Fetching current user");

        if (_httpContextAccessor.HttpContext != null)
        {
            ClaimsPrincipal user = _httpContextAccessor.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                Claim? userIdClaim = user.FindFirst("UserId");
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    _logger.LogInfo($"Current user ID retrieved successfully: {userId}");
                    return userId;
                }
            }
        }

        Guid fallbackUserId = _userContext.GetCurrentUserId();
        if (fallbackUserId != Guid.Empty)
        {
            return fallbackUserId;
        }

        return Guid.Empty;
    }
}
