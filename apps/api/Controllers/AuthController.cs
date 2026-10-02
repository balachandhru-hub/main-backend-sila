using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
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
    ITenantContextAccessor tenants,
    SilaMeDbContext db) : ControllerBase
{
    // Procurement users who open SILA ME as bala@chervic.in. No second password.
    private static readonly HashSet<Guid> LinkedProcurementUsers =
    [
        Guid.Parse("6f14b6db-d888-44b7-8052-0a21f0265842"),
        Guid.Parse("6d07deca-4649-45db-88d5-dd9dceb3bf71"),
    ];

    private const string LinkedCloudEmail = "BALA@CHERVIC.IN";
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

    [HttpPost("cloud/procurement-bridge")]
    public async Task<IActionResult> ProcurementBridge(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("access_token", out var accessToken) || string.IsNullOrWhiteSpace(accessToken))
        {
            return Unauthorized(new ApiError("PROCUREMENT_SESSION_REQUIRED", "Sign in to procurement first."));
        }

        var procurementUserId = await ProcurementUserIdAsync(accessToken, cancellationToken);
        if (procurementUserId is null)
        {
            return Unauthorized(new ApiError("PROCUREMENT_SESSION_REQUIRED", "Sign in to procurement first."));
        }

        if (!LinkedProcurementUsers.Contains(procurementUserId.Value))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("SILA_ME_NOT_LINKED", "This user is not linked to SILA ME."));
        }

        var cloudUser = await db.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == LinkedCloudEmail, cancellationToken);
        if (cloudUser is null || cloudUser.Status != StatusKind.ACTIVE)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("SILA_ME_NOT_LINKED", "The linked SILA ME user is not active."));
        }

        var session = await sessionService.CreateAsync(cloudUser, ApplicationKind.CLOUD, cancellationToken);
        SetCloudCookie(session.RawToken);
        return Ok(new { email = cloudUser.Email });
    }

    // ponytail: identity stays on 127.0.0.1:8001; point this at the gateway if identity moves.
    private static async Task<Guid?> ProcurementUserIdAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://127.0.0.1:8001/api/v1/identity/token-claim");
        request.Headers.TryAddWithoutValidation("Cookie", $"access_token={accessToken}");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("userId", out var userId) || !Guid.TryParse(userId.GetString(), out var parsed))
        {
            return null;
        }

        return parsed;
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
