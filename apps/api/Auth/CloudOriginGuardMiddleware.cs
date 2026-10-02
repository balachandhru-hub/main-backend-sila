using Microsoft.AspNetCore.Http;
using SilaMe.Api.DTOs;

namespace SilaMe.Api.Auth;

public sealed class CloudOriginGuardMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> UnsafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (UnsafeMethods.Contains(context.Request.Method)
            && (context.Request.Path.StartsWithSegments("/api/auth")
                || context.Request.Path.StartsWithSegments("/api/platform")
                || context.Request.Path.StartsWithSegments("/api/v1")
                || context.Request.Path.StartsWithSegments("/api/me")))
        {
            var origin = context.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(origin) && !OriginMatches(origin, context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ApiError("CSRF_REJECTED", "The request origin is not allowed for this Cloud action."));
                return;
            }
        }

        await next(context);
    }

    private static bool OriginMatches(string origin, HttpRequest request)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = request.Host.Host;
        return string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase)
            || (IsLoopback(uri.Host) && IsLoopback(host));
    }

    private static bool IsLoopback(string host) =>
        host is "localhost" or "127.0.0.1" or "::1";
}
