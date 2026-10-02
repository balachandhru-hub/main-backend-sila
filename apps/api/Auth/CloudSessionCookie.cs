using Microsoft.Extensions.Configuration;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Auth;

public static class CloudSessionCookie
{
    public const string Name = "sila_me_session";
    public const int DefaultLifetimeDays = 90;

    public static int LifetimeDays(IConfiguration configuration) =>
        Math.Max(1, configuration.GetValue("Authentication:SessionDays", DefaultLifetimeDays));

    public static TimeSpan Lifetime(IConfiguration configuration) =>
        TimeSpan.FromDays(LifetimeDays(configuration));

    public static string NameFor(string? routeSlug) =>
        string.IsNullOrWhiteSpace(routeSlug) ? Name : $"{Name}.{TenantRouteResolver.NormalizeSlug(routeSlug)}";

    public static CookieOptions Options(HttpRequest request, IConfiguration configuration)
    {
        var lifetime = Lifetime(configuration);
        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = request.IsHttps,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.Add(lifetime),
            MaxAge = lifetime,
            Path = "/",
        };
    }
}
