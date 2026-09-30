using Contracts.IRepository;
using HashingSystem;
using Identity.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBcryptHashing _hashing;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public LogoutCommandHandler(
            IRepositoryWrapper repository,
            IHttpContextAccessor httpContextAccessor,
            IBcryptHashing hashing,
            ILoggerManager logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _hashing = hashing;
            _logger = logger;
            _configuration = configuration;
        }
 
        public async Task<bool> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
 
            if (httpContext == null)
            {
                _logger.LogError("HTTP context is null.");
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Invalid request.");
            }
 
            // Current logged in user from JWT
            string? userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
 
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                _logger.LogError("User is not authenticated.");
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "User not authenticated.");
            }
 
            Guid userId = Guid.Parse(userIdClaim);
 
            // Refresh token from cookie
            string? refreshTokenCookie =
                httpContext.Request.Cookies[Common.COOKIE_REFRESH_TOKEN_KEY];
 
            if (string.IsNullOrWhiteSpace(refreshTokenCookie))
            {
                _logger.LogError("Refresh token not found.");
                throw new NotFoundCustomException(
                    "Refresh token not found.",
                    "User already logged out.");
            }
 
            _logger.LogInfo($"Fetching active refresh token for user : {userId}");
 
            var refreshTokens = _repository.RefreshToken
                .FindByCondition(x => x.UserId == userId && x.IsActive)
                .ToList();
 
            var refreshToken = refreshTokens.FirstOrDefault(x =>
                _hashing.VerifyHash(refreshTokenCookie, x.Token));
 
            if (refreshToken == null)
            {
                _logger.LogError("Invalid refresh token.");
                throw new NotFoundCustomException(
                    "Refresh token not found.",
                    "Invalid refresh token.");
            }
 
            _logger.LogInfo($"Logging out user : {userId}");
 
            refreshToken.IsActive = false;
 
            _repository.RefreshToken.Update(refreshToken);
 
            await _repository.SaveAsync();
 
            // A cookie delete only takes effect in the browser when its
            // Path/Domain match how the cookie was originally set - /login
            // sets access_token/refresh_token host-only (no Domain), while
            // /refresh-token sets them with the configured Domain. Clear
            // both variants so logout works regardless of which endpoint
            // last issued the cookies.
            var cookieDomain = _configuration[Common.DOMAIN_COOKIE_NAME];

            httpContext.Response.Cookies.Delete(
                Common.COOKIE_ACCESS_TOKEN_KEY,
                new CookieOptions { Path = "/" });
            httpContext.Response.Cookies.Delete(
                Common.COOKIE_REFRESH_TOKEN_KEY,
                new CookieOptions { Path = "/" });

            if (!string.IsNullOrWhiteSpace(cookieDomain))
            {
                httpContext.Response.Cookies.Delete(
                    Common.COOKIE_ACCESS_TOKEN_KEY,
                    new CookieOptions { Path = "/", Domain = cookieDomain });
                httpContext.Response.Cookies.Delete(
                    Common.COOKIE_REFRESH_TOKEN_KEY,
                    new CookieOptions { Path = "/", Domain = cookieDomain });
            }
 
            _logger.LogInfo($"User logged out successfully : {userId}");
 
            return true;
        }
    }
}