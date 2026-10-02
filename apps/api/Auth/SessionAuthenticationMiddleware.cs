using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Auth;

public static class SessionContext
{
    public const string ItemKey = "SilaMe.Session";
}

public sealed class SessionAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ISessionService sessions,
        IConfiguration configuration,
        SilaMe.Api.Tenancy.ITenantContextAccessor tenants)
    {
        var bearerToken = context.Request.Headers.Authorization.ToString();
        var cookieName = CloudSessionCookie.NameFor(tenants.Current?.RouteSlug);
        var rawToken = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken["Bearer ".Length..].Trim()
            : context.Request.Cookies[cookieName] ?? context.Request.Cookies[CloudSessionCookie.Name];
        var application = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? ApplicationKind.MOBILE
            : ApplicationKind.CLOUD;

        var session = await sessions.ValidateAsync(rawToken, application, context.RequestAborted);
        if (session is not null)
        {
            context.Items[SessionContext.ItemKey] = session;
            if (application == ApplicationKind.CLOUD && !string.IsNullOrWhiteSpace(rawToken))
                context.Response.Cookies.Append(cookieName, rawToken, CloudSessionCookie.Options(context.Request, configuration));
        }

        await next(context);
    }
}
