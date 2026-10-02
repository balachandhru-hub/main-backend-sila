using Microsoft.AspNetCore.Mvc;
using SilaMe.Api.Auth;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    ISessionService sessionService,
    IUserService userService,
    IConfiguration configuration,
    ITenantLaunchService launches,
    ITenantContextAccessor tenants) : ControllerBase
{
    [HttpPost("cloud/login")]
    public async Task<IActionResult> CloudLogin([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, ApplicationKind.CLOUD, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ApiError("INVALID_CREDENTIALS", "Invalid credentials."));
        }

        var login = result.Value;
        SetCloudCookie(login.RawToken);
        return Ok(new AuthResponse(userService.ToResponse(login.User, ApplicationKind.CLOUD, login.Session)));
    }

    [HttpGet("cloud/session")]
    public IActionResult CloudSession()
    {
        var session = HttpContext.Items[SessionContext.ItemKey] as Session;
        return session is null
            ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."))
            : Ok(new AuthResponse(userService.ToResponse(session.User, ApplicationKind.CLOUD, session)));
    }

    [HttpPost("cloud/logout")]
    public async Task<IActionResult> CloudLogout(CancellationToken cancellationToken)
    {
        await sessionService.RevokeAsync(HttpContext.Items[SessionContext.ItemKey] as Session, cancellationToken);
        ClearCloudCookies();
        return NoContent();
    }

    [HttpPost("mobile/login")]
    public async Task<IActionResult> MobileLogin([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, ApplicationKind.MOBILE, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ApiError("INVALID_CREDENTIALS", "Invalid credentials."));
        }

        var login = result.Value;
        return Ok(new MobileAuthResponse(userService.ToResponse(login.User, ApplicationKind.MOBILE, login.Session), login.RawToken));
    }

    [HttpPost("mobile/logout")]
    public async Task<IActionResult> MobileLogout(CancellationToken cancellationToken)
    {
        await sessionService.RevokeAsync(HttpContext.Items[SessionContext.ItemKey] as Session, cancellationToken);
        return NoContent();
    }

    [HttpPost("launch")]
    public async Task<IActionResult> ConsumeLaunch([FromBody] LaunchConsumeRequest request, CancellationToken cancellationToken)
    {
        var hostname = TenantResolutionMiddleware.ResolveHostname(HttpContext);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await launches.ConsumeAsync(request.Code, hostname, ip, cancellationToken);
        SetCloudCookie(result.RawToken);
        return Ok(new AuthResponse(userService.ToResponse(result.Session.User, ApplicationKind.CLOUD, result.Session)));
    }

    [HttpPost("support/end")]
    public async Task<IActionResult> EndSupportSession(CancellationToken cancellationToken)
    {
        var session = HttpContext.Items[SessionContext.ItemKey] as Session;
        if (session is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        if (!session.IsSupportSession)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PLATFORM_ACCESS_DENIED", "This session is not a SILA Platform support session."));
        }

        await launches.EndSupportSessionAsync(session, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        ClearCloudCookies();
        return NoContent();
    }

    private void SetCloudCookie(string rawToken)
    {
        Response.Cookies.Append(
            CloudSessionCookie.NameFor(tenants.Current?.RouteSlug),
            rawToken,
            CloudSessionCookie.Options(Request, configuration));
    }

    private void ClearCloudCookies()
    {
        var options = new CookieOptions { Path = "/" };
        Response.Cookies.Delete(CloudSessionCookie.Name, options);
        Response.Cookies.Delete(CloudSessionCookie.NameFor(tenants.Current?.RouteSlug), options);
    }
}
