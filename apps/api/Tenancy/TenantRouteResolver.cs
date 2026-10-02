using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.Models;

namespace SilaMe.Api.Tenancy;

public static class TenantRouteResolver
{
    public const string HeaderName = "X-Sila-Route-Slug";

    public static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "api", "platform", "organizations", "login", "dashboard", "admin", "approvals",
        "analytics", "documents", "inventory", "receiving", "purchasing", "procurement",
        "master-data", "menu-engineering", "assets", "src",
    };

    public static string NormalizeSlug(string value) => value.Trim().Trim('/').ToLowerInvariant();

    public static string CanonicalizeSlug(string value)
    {
        var normalized = NormalizeSlug(value);
        return string.Equals(normalized, TenantOperationalDatabase.FiveLegacyTestRouteAlias, StringComparison.OrdinalIgnoreCase)
            ? TenantOperationalDatabase.FiveCustomerRouteSlug
            : normalized;
    }

    public static string? FromRequest(HttpContext context)
    {
        var header = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header))
        {
            var normalized = CanonicalizeSlug(header);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        var fromPath = SlugFromPath(context.Request.Path.Value);
        if (!string.IsNullOrWhiteSpace(fromPath))
        {
            return CanonicalizeSlug(fromPath);
        }

        var referer = context.Request.Headers.Referer.FirstOrDefault();
        var fromReferer = SlugFromUrl(referer);
        return string.IsNullOrWhiteSpace(fromReferer) ? null : CanonicalizeSlug(fromReferer);
    }

    public static string? SlugFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var first = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return IsUsableSlug(first) ? NormalizeSlug(first!) : null;
    }

    public static string? SlugFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            var path = url.Trim().Trim('/');
            var first = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return IsUsableSlug(first) ? NormalizeSlug(first!) : null;
        }

        var segment = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (IsUsableSlug(segment))
        {
            return NormalizeSlug(segment!);
        }

        var host = uri.Host.ToLowerInvariant();
        if (host is "localhost" or "127.0.0.1") return null;
        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length >= 3 && IsUsableSlug(labels[0]))
        {
            return NormalizeSlug(labels[0]);
        }

        return null;
    }

    public static bool IsUsableSlug(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !ReservedSlugs.Contains(value)
        && System.Text.RegularExpressions.Regex.IsMatch(value, "^[a-z0-9][a-z0-9-]{0,62}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public static string CookiePath(string? routeSlug) =>
        string.IsNullOrWhiteSpace(routeSlug) ? "/" : "/" + NormalizeSlug(routeSlug);

    public static async Task<TenantEnvironment?> FindEnvironmentBySlugAsync(
        PlatformDbContext platform,
        string slug,
        CancellationToken cancellationToken)
    {
        var normalized = CanonicalizeSlug(slug);
        return await platform.TenantEnvironments
            .Include(item => item.Tenant)
            .Include(item => item.Domains)
            .SingleOrDefaultAsync(item => item.RouteSlug == normalized, cancellationToken);
    }
}
