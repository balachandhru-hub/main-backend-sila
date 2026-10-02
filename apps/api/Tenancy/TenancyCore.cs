using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Tenancy;

public sealed class TenantException(string code, string message, int statusCode = StatusCodes.Status403Forbidden)
    : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}

public sealed record TenantContext(
    Guid TenantId,
    string TenantCode,
    string CustomerName,
    TenantStatus TenantStatus,
    Guid EnvironmentId,
    TenantEnvironmentType EnvironmentType,
    TenantEnvironmentStatus EnvironmentStatus,
    string Hostname,
    string DatabaseSecretReference,
    string BaseUrl,
    string DataRegion,
    string? RouteSlug);

public interface ITenantContextAccessor
{
    TenantContext? Current { get; set; }
}

public sealed class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> current = new();
    public TenantContext? Current { get => current.Value; set => current.Value = value; }
}

public sealed record OperationalDatabaseOptions(string ConnectionString);

public interface ISecretProvider
{
    string Resolve(string secretReference);
}

public sealed class EnvironmentSecretProvider(OperationalDatabaseOptions operational) : ISecretProvider
{
    public string Resolve(string secretReference)
    {
        if (string.IsNullOrWhiteSpace(secretReference))
        {
            throw new TenantException("TENANT_DATABASE_UNAVAILABLE", "A database secret reference is missing.", StatusCodes.Status503ServiceUnavailable);
        }

        if (secretReference is "dev-operational" or "bootstrap-operational")
        {
            return operational.ConnectionString;
        }

        var envName = "SILA_SECRET_" + secretReference.Replace('-', '_').ToUpperInvariant();
        var value = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new TenantException("TENANT_DATABASE_UNAVAILABLE", "The tenant database secret is not configured.", StatusCodes.Status503ServiceUnavailable);
    }
}

public interface ITenantDatabaseResolver
{
    string GetConnectionString(TenantContext context);
    string GetConnectionString(string secretReference);
}

public sealed class TenantDatabaseResolver(ISecretProvider secrets) : ITenantDatabaseResolver
{
    private readonly ConcurrentDictionary<string, string> cache = new(StringComparer.Ordinal);

    public string GetConnectionString(TenantContext context) => GetConnectionString(context.DatabaseSecretReference);

    public string GetConnectionString(string secretReference)
    {
        return cache.GetOrAdd(secretReference, key =>
        {
            var connection = DatabaseUrl.Normalize(secrets.Resolve(key));
            var database = new NpgsqlConnectionStringBuilder(connection).Database;
            if (TenantOperationalDatabase.IsFiveOperationalSecret(key)
                && string.Equals(database, TenantOperationalDatabase.LegacyOperationalDatabaseName, StringComparison.OrdinalIgnoreCase))
            {
                throw new TenantException(
                    "TENANT_DATABASE_UNAVAILABLE",
                    "FIVE operational traffic cannot use the legacy SILA-DEV database.",
                    StatusCodes.Status503ServiceUnavailable);
            }

            return connection;
        });
    }
}

public static class TenantCacheKey
{
    public static string For(TenantContext context, string suffix) =>
        $"tenant:{context.TenantId:D}:env:{context.EnvironmentId:D}:{suffix}";
}

public static class PlatformPermissionKeys
{
    public const string TenantsRead = "platform.tenants.read";
    public const string TenantsManage = "platform.tenants.manage";
    public const string TenantsProvision = "platform.tenants.provision";
    public const string TenantsLaunch = "platform.tenants.launch";
    public const string TenantsSuspend = "platform.tenants.suspend";
    public const string LicensesManage = "platform.licenses.manage";
    public const string EntitlementsManage = "platform.entitlements.manage";
    public const string PlatformUsersManage = "platform.users.manage";
    public const string SupportAssign = "platform.support.assign";
    public const string AuditRead = "platform.audit.read";
    public const string ViewTenant = "VIEW_TENANT";
    public const string LaunchTenant = "LAUNCH_TENANT";
    public const string ViewConfiguration = "VIEW_CONFIGURATION";
    public const string ViewIntegration = "VIEW_INTEGRATION";
    public const string ViewLogs = "VIEW_LOGS";
    public const string ReprocessIntegration = "REPROCESS_INTEGRATION";
    public const string SupportUser = "SUPPORT_USER";

    public static readonly string[] All =
    [
        TenantsRead, TenantsManage, TenantsProvision, TenantsLaunch, TenantsSuspend,
        LicensesManage, EntitlementsManage, PlatformUsersManage, SupportAssign, AuditRead,
        ViewTenant, LaunchTenant, ViewConfiguration, ViewIntegration, ViewLogs, ReprocessIntegration, SupportUser,
    ];
}

public static class ProductCodes
{
    public const string SilaMe = "SILA_ME";
    public static readonly string[] DefaultModules =
    [
        "MENU_ENGINEERING", "RECEIVING", "INVENTORY", "LIVE_STOCK", "ITO",
        "STOCK_COUNT", "GOODS_ISSUE", "DAMAGE", "DOCUMENTS", "APPROVALS",
    ];
}

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        PlatformDbContext platform,
        ITenantContextAccessor accessor)
    {
        if (HttpMethods.Options.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (IsPlatformPath(path) || IsAnonymousInfrastructure(path))
        {
            await next(context);
            return;
        }

        if (context.Request.Query.ContainsKey("tenantId")
            || context.Request.Query.ContainsKey("tenant")
            || context.Request.Query.ContainsKey("database")
            || context.Request.Query.ContainsKey("databaseName"))
        {
            // Client-supplied tenant/database selectors are ignored. Resolution is route/session only.
        }

        var slug = TenantRouteResolver.FromRequest(context);
        var authorization = context.Request.Headers.Authorization.ToString();
        var hasBearer = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var isMobileAuth = path.StartsWith("/api/auth/mobile/", StringComparison.OrdinalIgnoreCase)
            && !HttpMethods.Options.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(slug) && (hasBearer || isMobileAuth))
        {
            throw new TenantException(
                "TENANT_CONTEXT_REQUIRED",
                "Mobile requests require a customer route. Legacy localhost fallback is not used.",
                StatusCodes.Status401Unauthorized);
        }

        TenantEnvironment? environment = null;
        TenantDomain? domain = null;
        if (!string.IsNullOrWhiteSpace(slug))
        {
            environment = await TenantRouteResolver.FindEnvironmentBySlugAsync(platform, slug, context.RequestAborted);
            if (environment is null)
            {
                throw new TenantException("TENANT_DOMAIN_NOT_FOUND", "This customer route is not registered to a SILA organization.", StatusCodes.Status404NotFound);
            }
        }
        else
        {
            var hostname = ResolveHostname(context);
            if (hostname is "localhost" or "127.0.0.1")
            {
                throw new TenantException(
                    "TENANT_CONTEXT_REQUIRED",
                    "Customer requests require a customer route. Localhost does not fall back to SILA-DEV.",
                    StatusCodes.Status401Unauthorized);
            }

            domain = await platform.TenantDomains
                .AsNoTracking()
                .Include(item => item.Tenant)
                .Include(item => item.Environment)
                .SingleOrDefaultAsync(item => item.Hostname == hostname && item.IsVerified, context.RequestAborted);
            if (domain is null)
            {
                throw new TenantException("TENANT_DOMAIN_NOT_FOUND", "This hostname is not registered to a SILA tenant.", StatusCodes.Status404NotFound);
            }

            environment = domain.Environment;
            environment.Tenant = domain.Tenant;
        }

        var tenant = environment.Tenant;
        if (tenant.Status == TenantStatus.SUSPENDED)
        {
            throw new TenantException("TENANT_SUSPENDED", "Users will not be able to access this organization's customer applications while the organization is suspended.");
        }

        if (tenant.Status is TenantStatus.ARCHIVED)
        {
            throw new TenantException("TENANT_NOT_FOUND", "This organization is not available.");
        }

        if (tenant.Status != TenantStatus.ACTIVE)
        {
            throw new TenantException("TENANT_NOT_FOUND", "This tenant is not available.");
        }

        if (environment.Status is TenantEnvironmentStatus.SUSPENDED or TenantEnvironmentStatus.FAILED or TenantEnvironmentStatus.PROVISIONING
            || !environment.IsActive)
        {
            throw new TenantException("ENVIRONMENT_NOT_ACTIVE", "This tenant environment is not active.");
        }

        var hostnameLabel = domain?.Hostname ?? slug ?? ResolveHostname(context);
        accessor.Current = new TenantContext(
            tenant.Id,
            tenant.TenantCode,
            tenant.CustomerName,
            tenant.Status,
            environment.Id,
            environment.EnvironmentType,
            environment.Status,
            hostnameLabel,
            environment.DatabaseSecretReference,
            environment.BaseUrl,
            environment.DataRegion,
            environment.RouteSlug);
        context.Items["SilaMe.Tenant"] = accessor.Current;
        try
        {
            await next(context);
        }
        finally
        {
            accessor.Current = null;
        }
    }

    internal static string ResolveHostname(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        var raw = !string.IsNullOrWhiteSpace(forwarded) ? forwarded : context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new TenantException("TENANT_DOMAIN_NOT_FOUND", "This hostname is not registered to a SILA tenant.", StatusCodes.Status404NotFound);
        }

        var host = raw.Split(',')[0].Trim().ToLowerInvariant();
        var colon = host.IndexOf(':');
        return colon > 0 ? host[..colon] : host;
    }

    private static bool IsPlatformPath(string path) =>
        path.StartsWith("/api/platform", StringComparison.OrdinalIgnoreCase);

    private static bool IsAnonymousInfrastructure(string path) =>
        path.Equals("/api/healthz", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/dev/database-status", StringComparison.OrdinalIgnoreCase);
}

public sealed class TenantExceptionMiddleware(RequestDelegate next, ILogger<TenantExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (TenantException exception)
        {
            logger.LogInformation("Tenant request rejected Code={Code} Path={Path}", exception.Code, context.Request.Path.Value);
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = exception.StatusCode;
            await context.Response.WriteAsJsonAsync(new ApiError(exception.Code, exception.Message));
        }
    }
}

public interface ILicenseService
{
    Task EnsureCanActivateUserAsync(ApplicationKind application, CancellationToken cancellationToken);
    Task<IReadOnlyList<object>> GetUsageAsync(CancellationToken cancellationToken);
}

public sealed class LicenseService(
    PlatformDbContext platform,
    SilaMeDbContext tenantDb,
    ITenantContextAccessor tenants) : ILicenseService
{
    public async Task EnsureCanActivateUserAsync(ApplicationKind application, CancellationToken cancellationToken)
    {
        var context = tenants.Current ?? throw new TenantException("TENANT_NOT_FOUND", "Tenant context is missing.");
        var licenseType = application == ApplicationKind.CLOUD ? "CLOUD_USERS" : "MOBILE_USERS";
        var license = await platform.TenantLicenses.AsNoTracking()
            .Where(item => item.TenantId == context.TenantId && item.LicenseType == licenseType && item.Status == LicenseStatus.ACTIVE)
            .OrderByDescending(item => item.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);
        var limit = license?.LicensedQuantity
            ?? (application == ApplicationKind.CLOUD
                ? (await platform.Tenants.AsNoTracking().SingleAsync(item => item.Id == context.TenantId, cancellationToken)).MaxCloudUsers
                : (await platform.Tenants.AsNoTracking().SingleAsync(item => item.Id == context.TenantId, cancellationToken)).MaxMobileUsers);
        var used = await tenantDb.UserApplicationAccess.CountAsync(
            item => item.Application == application
                && item.Status == StatusKind.ACTIVE
                && !item.User.NormalizedEmail.EndsWith("@SILAME.INTERNAL"),
            cancellationToken);
        if (used >= limit)
        {
            throw new TenantException("LICENSE_LIMIT_REACHED", "The tenant has no remaining licenses for this application.", StatusCodes.Status409Conflict);
        }
    }

    public async Task<IReadOnlyList<object>> GetUsageAsync(CancellationToken cancellationToken)
    {
        var context = tenants.Current ?? throw new TenantException("TENANT_NOT_FOUND", "Tenant context is missing.");
        var licenses = await platform.TenantLicenses.AsNoTracking()
            .Where(item => item.TenantId == context.TenantId)
            .ToListAsync(cancellationToken);
        var cloudUsed = await tenantDb.UserApplicationAccess.CountAsync(item => item.Application == ApplicationKind.CLOUD && item.Status == StatusKind.ACTIVE && !item.User.NormalizedEmail.EndsWith("@SILAME.INTERNAL"), cancellationToken);
        var mobileUsed = await tenantDb.UserApplicationAccess.CountAsync(item => item.Application == ApplicationKind.MOBILE && item.Status == StatusKind.ACTIVE && !item.User.NormalizedEmail.EndsWith("@SILAME.INTERNAL"), cancellationToken);
        return licenses.Select(item => (object)new
        {
            item.LicenseType,
            item.ProductCode,
            item.LicensedQuantity,
            UsedQuantity = item.LicenseType == "MOBILE_USERS" ? mobileUsed : cloudUsed,
            item.Status,
            item.ValidFrom,
            item.ValidUntil,
        }).ToList();
    }
}

public interface IEntitlementService
{
    Task EnsureProductAsync(string productCode, CancellationToken cancellationToken);
    Task EnsureModuleAsync(string moduleCode, CancellationToken cancellationToken);
    Task<(IReadOnlyList<string> Products, IReadOnlyList<string> Modules)> ListAsync(CancellationToken cancellationToken);
}

public sealed class EntitlementService(PlatformDbContext platform, ITenantContextAccessor tenants) : IEntitlementService
{
    public async Task EnsureProductAsync(string productCode, CancellationToken cancellationToken)
    {
        var context = Require();
        var entitled = await platform.TenantProductEntitlements.AsNoTracking().AnyAsync(
            item => item.TenantId == context.TenantId && item.ProductCode == productCode && item.Status == EntitlementStatus.ACTIVE, cancellationToken);
        if (!entitled)
        {
            throw new TenantException("PRODUCT_NOT_ENTITLED", "This product is not entitled for the tenant.");
        }
    }

    public async Task EnsureModuleAsync(string moduleCode, CancellationToken cancellationToken)
    {
        var context = Require();
        var entitled = await platform.TenantModuleEntitlements.AsNoTracking().AnyAsync(
            item => item.TenantId == context.TenantId && item.ModuleCode == moduleCode && item.Status == EntitlementStatus.ACTIVE, cancellationToken);
        if (!entitled)
        {
            throw new TenantException("MODULE_NOT_ENTITLED", "This module is not entitled for the tenant.");
        }
    }

    public async Task<(IReadOnlyList<string> Products, IReadOnlyList<string> Modules)> ListAsync(CancellationToken cancellationToken)
    {
        var context = Require();
        var products = await platform.TenantProductEntitlements.AsNoTracking()
            .Where(item => item.TenantId == context.TenantId && item.Status == EntitlementStatus.ACTIVE)
            .Select(item => item.ProductCode).ToListAsync(cancellationToken);
        var modules = await platform.TenantModuleEntitlements.AsNoTracking()
            .Where(item => item.TenantId == context.TenantId && item.Status == EntitlementStatus.ACTIVE)
            .Select(item => item.ModuleCode).ToListAsync(cancellationToken);
        return (products, modules);
    }

    private TenantContext Require() =>
        tenants.Current ?? throw new TenantException("TENANT_NOT_FOUND", "Tenant context is missing.");
}

public sealed class TenantJobRunner(IServiceScopeFactory scopes, ILogger<TenantJobRunner> logger)
{
    public async Task ForEachActiveEnvironmentAsync(Func<IServiceProvider, TenantContext, CancellationToken, Task> work, CancellationToken cancellationToken)
    {
        List<TenantEnvironment> environments;
        await using (var listing = scopes.CreateAsyncScope())
        {
            var platform = listing.ServiceProvider.GetRequiredService<PlatformDbContext>();
            environments = await platform.TenantEnvironments.AsNoTracking()
                .Include(item => item.Tenant)
                .Where(item => item.IsActive && item.Status == TenantEnvironmentStatus.ACTIVE && item.Tenant.Status == TenantStatus.ACTIVE)
                .ToListAsync(cancellationToken);
        }

        foreach (var environment in environments)
        {
            await using var scope = scopes.CreateAsyncScope();
            var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
            accessor.Current = new TenantContext(
                environment.TenantId, environment.Tenant.TenantCode, environment.Tenant.CustomerName, environment.Tenant.Status,
                environment.Id, environment.EnvironmentType, environment.Status, "worker", environment.DatabaseSecretReference, environment.BaseUrl, environment.DataRegion, environment.RouteSlug);
            try
            {
                await work(scope.ServiceProvider, accessor.Current, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Tenant job failed Tenant={TenantId} Environment={EnvironmentId}", environment.TenantId, environment.Id);
            }
            finally
            {
                accessor.Current = null;
            }
        }
    }
}
