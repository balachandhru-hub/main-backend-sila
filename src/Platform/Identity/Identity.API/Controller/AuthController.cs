using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Identity.Application.Features.Auth.Commands.SendEmailVerification;
using SharedKernel.LoggerServices;
using Identity.Application.Features.Auth.Commands.VerifyOtp;
using SharedKernel.Dto;
using SharedKernel.Attributes;
using Identity.API.Attributes;
using Identity.Application.Features.Commands.Login;
using Identity.Application.Features.Commands.Register;
using Identity.Application.Features.Commands.RefreshToken.RefreshToken;
using Identity.Domain.Common;
using Identity.Domain.Dto;
using Identity.Application.Features.Auth.Queries.GetClaim;
using Identity.Application.Features.Commands.Logout;



namespace Identity.API.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;



        public AuthController(
            IMediator mediator,
            ILoggerManager logger,
            HttpClient httpClient,
            IConfiguration configuration

            )
        {
            _mediator = mediator;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;

        }

        /// <summary>
        /// Generates OTP for email verification.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/v1/identity/auth/send-otp")]
        [ApiKeyAuthorization]
        [ValidateModelState]
        [SwaggerOperation("SendOtp")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "OTP generated successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> SendOtp([FromBody] SendEmailVerificationCommand command)
        {
            _logger.LogInfo($"Generating OTP for {command.Email}");

            var result = await _mediator.Send(command);

            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "OTP generated successfully.",
                Description = "OTP generated successfully."
            });
        }

        /// <summary>
        /// Verifies the OTP for email verification.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>

        [HttpPost]
        [Route("api/v1/identity/auth/verify-otp")]
        [ValidateModelState]
        [ApiKeyAuthorization]
        [SwaggerOperation("VerifyOtp")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "OTP verified successfully.")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Invalid OTP or OTP expired.")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error.")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpCommand command)
        {
            var result = await _mediator.Send(command);

            Response.Cookies.Append(Common.VERIFICATION_TOKEN_COOKIE_NAME, result.TemporaryVerificationToken!,
            new CookieOptions
            {
                Domain = _configuration[Common.DOMAIN_COOKIE_NAME],
                Path = "/",
                HttpOnly = true,
                Secure = true,          // Use true in HTTPS
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                IsEssential = true
            });

            return Ok(new SuccessResponseDto
            {
                Message = result.Message,
                Description = "OTP verified successfully.",
                StatusCode = 200
            });
        }
        /// <summary>
        /// Login
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/v1/identity/auth/login")]
        [ValidateModelState]
        [SwaggerOperation("Login")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Login successful.")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad Request")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            _logger.LogInfo($"Login request received for {command.UserName}");

            var result = await _mediator.Send(command);
            Response.Cookies.Append(Common.COOKIE_ACCESS_TOKEN_KEY, result.Token!,
                new CookieOptions
                {
                    // Domain = _configuration[Common.DOMAIN_COOKIE_NAME],
                    // Path = "/",
                    HttpOnly = true,
                    Secure = true,          // false for local HTTP, true for HTTPS
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddHours(1),
                    IsEssential = true
                });
            Response.Cookies.Append(
                Common.COOKIE_REFRESH_TOKEN_KEY,
                result.RefreshToken.ToString(),
                new CookieOptions
                {
                    // Domain = _configuration[Common.DOMAIN_COOKIE_NAME],
                    // Path = "/",
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>(Common.REFRESH_TOKEN_EXPIRATION_TIME)),
                    IsEssential = true
                });

            return Ok(new SuccessResponseDto { StatusCode = 200, Message = "Token Generated Successfully", Description = "Successfully Created Token and Refresh Token" });

        }

        /// <summary>
        /// Creates a new organization.
        /// </summary>

        [HttpPost]
        [Route("api/v1/identity/auth/register")]
        [ValidateModelState]
        [ApiKeyAuthorization]
        [SwaggerOperation("createOrganization")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Organization created successfully")]
        [SwaggerResponse(400, type: typeof(ErrorResponseDto), description: "Bad request")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> CreateOrganization([FromBody] RegisterCommand command)
        {
            _logger.LogInfo($"Creating organization for {command.Email}");
            var verificationToken = Request.Cookies[Common.VERIFICATION_TOKEN_COOKIE_NAME];
            command.VerificationToken = verificationToken;

            var organizationId = await _mediator.Send(command);

            return Ok(new SuccessResponseDto
            {
                Message = "Organization registered successfully.",
                Description = $"Organization Id: {organizationId}",
                StatusCode = 200
            });
        }

        /// <summary>
        /// Refresh Access Token
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("api/v1/identity/auth/refresh-token")]
        [SwaggerOperation("RefreshToken")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Token refreshed successfully.")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        [SwaggerResponse(500, type: typeof(ErrorResponseDto), description: "Internal Server Error")]
        public async Task<IActionResult> RefreshToken()
        {
            _logger.LogInfo("Refresh token request received.");

            string? refreshToken = Request.Cookies["refresh_token"];

            Guid.TryParse(refreshToken, out Guid parsedRefreshToken);

            var result = await _mediator.Send(new RefreshTokenCommand
            {
                RefreshToken = parsedRefreshToken
            });

            Response.Cookies.Append(
                Common.COOKIE_ACCESS_TOKEN_KEY,
                result.Token!,
                new CookieOptions
                {
                    // Domain = _configuration[Common.DOMAIN_COOKIE_NAME],
                    // Path = "/",
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddHours(1),
                    IsEssential = true
                });

            Response.Cookies.Append(
                Common.COOKIE_REFRESH_TOKEN_KEY,
                result.RefreshToken.ToString(),
                new CookieOptions
                {
                    // Domain = _configuration[Common.DOMAIN_COOKIE_NAME],
                    // Path = "/",
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTimeOffset.UtcNow.AddDays(_configuration.GetValue<int>(Common.REFRESH_TOKEN_EXPIRATION_TIME)),
                    IsEssential = true
                });

            return Ok(new SuccessResponseDto { StatusCode = 200, Message = "Token Generated Successfully", Description = "Successfully Created Token and Refresh Token" });
        }
        /// <summary>
        /// Token Claims
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/v1/identity/token-claim")]
        [ValidateModelState]
        [ApiAuthorization(Name ="GET_ALL_CLAIMS")]
        [SwaggerOperation("GetAllClaim")]
        [SwaggerResponse(200, type: typeof(TokenClaimDto), description: "Fetches logged in user claims")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        public async Task<IActionResult> GetAllClaim()
        {
            string? token = Request.Cookies[Common.COOKIE_ACCESS_TOKEN_KEY];
            TokenClaimDto result = await _mediator.Send(new GetClaimQuery
            {
                Token = token
            });

            return Ok(result);
        }

         /// <summary>
        /// Logout
        /// </summary>
        [HttpPut]
        [Route("api/v1/identity/auth/logout")]
        [ApiAuthorization(Name = "LOGOUT")]
        [SwaggerOperation("Logout")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto), description: "Logged out successfully")]
        [SwaggerResponse(401, type: typeof(ErrorResponseDto), description: "Unauthorized")]
        public async Task<IActionResult> Logout()
        {
            await _mediator.Send(new LogoutCommand());
 
            return Ok(new SuccessResponseDto
            {
                StatusCode = 200,
                Message = "Logged out successfully.",
                Description = "User logged out successfully."
            });
        }
    }
}