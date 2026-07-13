using Contracts.IRepository;
using HashingSystem;
using Identity.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
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
 
        public LogoutCommandHandler(
            IRepositoryWrapper repository,
            IHttpContextAccessor httpContextAccessor,
            IBcryptHashing hashing,
            ILoggerManager logger)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
            _hashing = hashing;
            _logger = logger;
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
 
            // Delete cookies
            httpContext.Response.Cookies.Delete(Common.COOKIE_ACCESS_TOKEN_KEY);
            httpContext.Response.Cookies.Delete(Common.COOKIE_REFRESH_TOKEN_KEY);
 
            _logger.LogInfo($"User logged out successfully : {userId}");
 
            return true;
        }
    }
}