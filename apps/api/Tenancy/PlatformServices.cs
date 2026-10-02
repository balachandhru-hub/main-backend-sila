using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Tenancy;

public sealed record ProvisionTenantRequest(
    string TenantCode,
    string CustomerName,
    string? LegalName,
    string? CountryCode,
    string? TaxRegistrationNumber,
    string? Address,
    string? City,
    string? PostalCode,
    string? PrimaryContactName,
    string? PrimaryContactEmail,
    string? PrimaryContactPhone,
    string? TimeZone,
    string? Currency,
    string? Language,
    int CloudUsers,
    int MobileUsers,
    DateTime? LicenseStart,
    DateTime? LicenseEnd,
    IReadOnlyList<string> Products,
    IReadOnlyList<string> Modules,
    bool CreateTest,
    bool CreateProduction,
    string? TestHostname,
    string? ProductionHostname,
    string? TestBaseUrl,
    string? ProductionBaseUrl,
    string AdminEmail,
    string AdminDisplayName,
    string AdminPassword,
    string? AdminFirstName,
    string? AdminLastName,
    IReadOnlyList<string> AdminApplications,
    int? LicenseCount = null,
    int? TotalUsers = null,
    string? ProductionRouteSlug = null,
    string? TestRouteSlug = null);

public interface ITenantProvisioningService
{
    Task<Tenant> ProvisionAsync(ProvisionTenantRequest request, Guid? actorId, CancellationToken cancellationToken);
    Task RegisterDevelopmentTenantAsync(string operationalConnectionString, CancellationToken cancellationToken);
}

public sealed class TenantProvisioningService(
    PlatformDbContext platform,
    OperationalDatabaseOptions operational,
    IPasswordService passwords,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task RegisterDevelopmentTenantAsync(string operationalConnectionString, CancellationToken cancellationToken)
    {
        const string code = "SILA-DEV";
        var existing = await platform.Tenants.Include(item => item.Environments).ThenInclude(item => item.Domains)
            .SingleOrDefaultAsync(item => item.TenantCode == code, cancellationToken);
        var now = DateTime.UtcNow;
        if (existing is null)
        {
            existing = new Tenant
            {
                Id = Guid.NewGuid(),
                TenantCode = code,
                NormalizedTenantCode = code,
                CustomerName = "SILA Development Tenant",
                LegalName = "SILA ME Local Development",
                CountryCode = "AE",
                DefaultTimeZone = "Asia/Dubai",
                DefaultCurrency = "AED",
                Status = TenantStatus.ACTIVE,
                MaxCloudUsers = 100,
                MaxMobileUsers = 500,
                CreatedAt = now,
                UpdatedAt = now,
            };
            platform.Tenants.Add(existing);
        }

        var environment = existing.Environments.FirstOrDefault(item => item.EnvironmentType == TenantEnvironmentType.DEVELOPMENT)
            ?? existing.Environments.FirstOrDefault(item => item.EnvironmentType == TenantEnvironmentType.TEST);
        if (environment is null)
        {
            environment = new TenantEnvironment
            {
                Id = Guid.NewGuid(),
                TenantId = existing.Id,
                EnvironmentType = TenantEnvironmentType.DEVELOPMENT,
                DisplayName = "Development",
                Status = TenantEnvironmentStatus.ACTIVE,
                BaseUrl = "http://localhost:5173",
                DatabaseSecretReference = "dev-operational",
                RouteSlug = "sila-dev",
                DataRegion = "LOCAL",
                IsActive = true,
                ProvisioningCheckpoint = ProvisioningCheckpoint.READY,
                CreatedAt = now,
                UpdatedAt = now,
            };
            platform.TenantEnvironments.Add(environment);
        }
        else
        {
            environment.Status = TenantEnvironmentStatus.ACTIVE;
            environment.IsActive = true;
            environment.DatabaseSecretReference = "dev-operational";
            if (string.IsNullOrWhiteSpace(environment.RouteSlug))
            {
                environment.RouteSlug = "sila-dev";
            }
            environment.ProvisioningCheckpoint = ProvisioningCheckpoint.READY;
            environment.UpdatedAt = now;
        }

        foreach (var hostname in new[] { "localhost", "127.0.0.1" })
        {
            if (!await platform.TenantDomains.AnyAsync(item => item.Hostname == hostname, cancellationToken))
            {
                platform.TenantDomains.Add(new TenantDomain
                {
                    Id = Guid.NewGuid(),
                    TenantId = existing.Id,
                    TenantEnvironmentId = environment.Id,
                    Hostname = hostname,
                    DomainType = TenantDomainType.LOOPBACK,
                    IsPrimary = hostname == "localhost",
                    IsVerified = true,
                    CreatedAt = now,
                });
            }
        }

        await EnsureLicenseAsync(existing.Id, "CLOUD_USERS", existing.MaxCloudUsers, now, cancellationToken);
        await EnsureLicenseAsync(existing.Id, "MOBILE_USERS", existing.MaxMobileUsers, now, cancellationToken);
        await EnsureProductAsync(existing.Id, ProductCodes.SilaMe, now, cancellationToken);
        foreach (var module in ProductCodes.DefaultModules)
        {
            await EnsureModuleAsync(existing.Id, ProductCodes.SilaMe, module, now, cancellationToken);
        }

        _ = operationalConnectionString;
        await platform.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered development tenant {TenantCode} against the existing operational database.", code);
    }

    public async Task<Tenant> ProvisionAsync(ProvisionTenantRequest request, Guid? actorId, CancellationToken cancellationToken)
    {
        var displayCode = request.TenantCode.Trim();
        var code = displayCode.ToUpperInvariant();
        var existing = await platform.Tenants.Include(item => item.Environments).ThenInclude(item => item.Domains)
            .SingleOrDefaultAsync(item => item.NormalizedTenantCode == code, cancellationToken);
        if (existing is { Status: TenantStatus.ACTIVE })
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var tenant = existing ?? new Tenant
        {
            Id = Guid.NewGuid(),
            TenantCode = displayCode,
            NormalizedTenantCode = code,
            CustomerName = request.CustomerName.Trim(),
            LegalName = request.LegalName?.Trim(),
            CountryCode = request.CountryCode?.Trim(),
            TaxRegistrationNumber = request.TaxRegistrationNumber?.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            PrimaryContactName = request.PrimaryContactName?.Trim(),
            PrimaryContactEmail = request.PrimaryContactEmail?.Trim(),
            PrimaryContactPhone = request.PrimaryContactPhone?.Trim(),
            DefaultTimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "UTC" : request.TimeZone.Trim(),
            DefaultCurrency = string.IsNullOrWhiteSpace(request.Currency) ? "AED" : request.Currency.Trim(),
            DefaultLanguage = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim(),
            ContractStartDate = request.LicenseStart,
            ContractEndDate = request.LicenseEnd,
            Status = TenantStatus.PROVISIONING,
            MaxCloudUsers = Math.Max(1, request.CloudUsers),
            MaxMobileUsers = Math.Max(1, request.MobileUsers),
            LicenseCount = request.LicenseCount ?? request.CloudUsers,
            TotalUsers = request.TotalUsers ?? 0,
            CreatedAt = now,
            CreatedByPlatformUserId = actorId,
            UpdatedAt = now,
            UpdatedByPlatformUserId = actorId,
        };
        if (existing is null)
        {
            platform.Tenants.Add(tenant);
            await platform.SaveChangesAsync(cancellationToken);
        }

        try
        {
            if (request.CreateTest || !string.IsNullOrWhiteSpace(request.TestHostname) || !string.IsNullOrWhiteSpace(request.TestBaseUrl) || !string.IsNullOrWhiteSpace(request.TestRouteSlug))
            {
                await ProvisionEnvironmentAsync(tenant, TenantEnvironmentType.TEST, request.TestHostname, request.TestBaseUrl, request.TestRouteSlug, request, actorId, cancellationToken);
            }

            if (request.CreateProduction || !string.IsNullOrWhiteSpace(request.ProductionHostname) || !string.IsNullOrWhiteSpace(request.ProductionBaseUrl) || !string.IsNullOrWhiteSpace(request.ProductionRouteSlug))
            {
                await ProvisionEnvironmentAsync(tenant, TenantEnvironmentType.PRODUCTION, request.ProductionHostname, request.ProductionBaseUrl, request.ProductionRouteSlug, request, actorId, cancellationToken);
            }

            tenant.Status = TenantStatus.ACTIVE;
            tenant.UpdatedAt = DateTime.UtcNow;
            await AuditAsync(actorId, "TENANT_CREATED", tenant.Id, null, cancellationToken);
            await platform.SaveChangesAsync(cancellationToken);
            return tenant;
        }
        catch (Exception exception)
        {
            tenant.Status = TenantStatus.PROVISIONING;
            logger.LogError(exception, "Tenant provisioning failed for {TenantCode}", code);
            await platform.SaveChangesAsync(cancellationToken);
            throw new TenantException("TENANT_DATABASE_UNAVAILABLE", "Tenant provisioning failed before the tenant could be activated.", StatusCodes.Status500InternalServerError);
        }
    }

    private async Task ProvisionEnvironmentAsync(
        Tenant tenant,
        TenantEnvironmentType type,
        string? hostname,
        string? baseUrl,
        string? routeSlug,
        ProvisionTenantRequest request,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var databaseName = TenantOperationalDatabase.Name(tenant.TenantCode, type);
        var secretReference = TenantOperationalDatabase.SecretReference(tenant.TenantCode, type);
        var environment = tenant.Environments.FirstOrDefault(item => item.EnvironmentType == type);
        if (environment is null)
        {
            environment = new TenantEnvironment
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                EnvironmentType = type,
                DisplayName = type.ToString(),
                Status = TenantEnvironmentStatus.PROVISIONING,
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? $"http://{hostname}" : baseUrl!,
                RouteSlug = string.IsNullOrWhiteSpace(routeSlug) ? TenantRouteResolver.SlugFromUrl(baseUrl) ?? TenantRouteResolver.SlugFromUrl(hostname) : TenantRouteResolver.NormalizeSlug(routeSlug),
                DatabaseSecretReference = secretReference,
                DataRegion = "LOCAL",
                IsActive = false,
                ProvisioningCheckpoint = ProvisioningCheckpoint.TENANT_CREATED,
                CreatedAt = now,
                UpdatedAt = now,
            };
            platform.TenantEnvironments.Add(environment);
            await platform.SaveChangesAsync(cancellationToken);
        }

        await TenantOperationalDatabase.EnsureDatabaseExistsAsync(operational.ConnectionString, databaseName, cancellationToken);
        environment.ProvisioningCheckpoint = ProvisioningCheckpoint.DATABASE_CREATED;
        Environment.SetEnvironmentVariable(TenantOperationalDatabase.SecretEnvironmentVariable(secretReference), ReplaceDatabase(operational.ConnectionString, databaseName));

        var connection = DatabaseUrl.Normalize(ReplaceDatabase(operational.ConnectionString, databaseName));
        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(connection).Options;
        await using (var tenantDb = new SilaMeDbContext(options))
        {
            await tenantDb.Database.MigrateAsync(cancellationToken);
            await AccessSeed.SeedCatalogAsync(tenantDb, DateTime.UtcNow);
            await tenantDb.SaveChangesAsync(cancellationToken);
            environment.LastAppliedMigration = (await tenantDb.Database.GetAppliedMigrationsAsync(cancellationToken)).LastOrDefault();
            environment.ProvisioningCheckpoint = ProvisioningCheckpoint.MIGRATIONS_APPLIED;
            await BootstrapTenantAdminAsync(tenantDb, request, cancellationToken);
            environment.ProvisioningCheckpoint = ProvisioningCheckpoint.ADMIN_CREATED;
        }

        var resolvedSlug = string.IsNullOrWhiteSpace(routeSlug)
            ? TenantRouteResolver.SlugFromUrl(environment.BaseUrl) ?? TenantRouteResolver.SlugFromUrl(hostname)
            : TenantRouteResolver.NormalizeSlug(routeSlug);
        if (!string.IsNullOrWhiteSpace(resolvedSlug))
        {
            environment.RouteSlug = resolvedSlug;
        }

        if (!string.IsNullOrWhiteSpace(hostname))
        {
            var host = hostname.Trim().ToLowerInvariant();
            var loopback = host is "localhost" or "127.0.0.1";
            if (!loopback && !await platform.TenantDomains.AnyAsync(item => item.Hostname == host, cancellationToken))
            {
                platform.TenantDomains.Add(new TenantDomain
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    TenantEnvironmentId = environment.Id,
                    Hostname = host,
                    DomainType = TenantDomainType.SILA_SUBDOMAIN,
                    IsPrimary = true,
                    IsVerified = true,
                    CreatedAt = now,
                });
            }
            environment.ProvisioningCheckpoint = ProvisioningCheckpoint.DOMAIN_REGISTERED;
        }

        await EnsureLicenseAsync(tenant.Id, "CLOUD_USERS", tenant.MaxCloudUsers, now, cancellationToken);
        await EnsureLicenseAsync(tenant.Id, "MOBILE_USERS", tenant.MaxMobileUsers, now, cancellationToken);
            var products = (request.Products ?? []).Where(item => !string.IsNullOrWhiteSpace(item)).DefaultIfEmpty(ProductCodes.SilaMe).Distinct();
            foreach (var product in products)
            {
                await EnsureProductAsync(tenant.Id, product, now, cancellationToken);
            }

            var modules = request.Modules is { Count: > 0 } ? request.Modules : ProductCodes.DefaultModules;
            foreach (var module in modules)
        {
            await EnsureModuleAsync(tenant.Id, ProductCodes.SilaMe, module, now, cancellationToken);
        }

        environment.ProvisioningCheckpoint = ProvisioningCheckpoint.ENTITLEMENTS_CREATED;
        environment.Status = TenantEnvironmentStatus.ACTIVE;
        environment.IsActive = true;
        environment.ProvisioningCheckpoint = ProvisioningCheckpoint.READY;
        environment.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(actorId, "ENVIRONMENT_CREATED", tenant.Id, environment.Id, cancellationToken);
        await platform.SaveChangesAsync(cancellationToken);
    }

    private async Task BootstrapTenantAdminAsync(SilaMeDbContext tenantDb, ProvisionTenantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AdminEmail) || string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            return;
        }
        var email = request.AdminEmail.Trim();
        var normalized = email.ToUpperInvariant();
        var user = await tenantDb.Users.Include(item => item.ApplicationAccess)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == normalized, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = normalized,
                DisplayName = request.AdminDisplayName.Trim(),
                PasswordHash = string.Empty,
                FirstName = request.AdminFirstName,
                LastName = request.AdminLastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = passwords.HashPassword(user, request.AdminPassword);
            tenantDb.Users.Add(user);
        }

        var applications = request.AdminApplications.Count == 0 ? new[] { "CLOUD" } : request.AdminApplications.ToArray();
        foreach (var applicationName in applications)
        {
            var application = Enum.Parse<ApplicationKind>(applicationName, true);
            if (user.ApplicationAccess.All(item => item.Application != application))
            {
                tenantDb.UserApplicationAccess.Add(new UserApplicationAccess
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Application = application,
                    Status = StatusKind.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }

        var organization = await tenantDb.Organizations.SingleOrDefaultAsync(item => item.Code == "DEFAULT", cancellationToken);
        if (organization is null)
        {
            organization = new Organization
            {
                Id = Guid.NewGuid(),
                Code = "DEFAULT",
                Name = request.CustomerName.Trim(),
                Kind = OrganizationKind.CUSTOMER,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            tenantDb.Organizations.Add(organization);
        }

        var adminRole = await tenantDb.Roles.SingleOrDefaultAsync(item => item.Key == "SUPER_ADMIN", cancellationToken);
        if (adminRole is not null && !await tenantDb.UserRoleAssignments.AnyAsync(item => item.UserId == user.Id && item.RoleId == adminRole.Id, cancellationToken))
        {
            tenantDb.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = adminRole.Id,
                OrganizationId = organization.Id,
                Status = StatusKind.ACTIVE,
                CreatedAt = DateTime.UtcNow,
            });
        }

        if (!await tenantDb.UserOrganizationMemberships.AnyAsync(item => item.UserId == user.Id && item.OrganizationId == organization.Id, cancellationToken))
        {
            tenantDb.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                Status = StatusKind.ACTIVE,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await tenantDb.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLicenseAsync(Guid tenantId, string type, int quantity, DateTime now, CancellationToken cancellationToken)
    {
        if (!await platform.TenantLicenses.AnyAsync(item => item.TenantId == tenantId && item.LicenseType == type, cancellationToken))
        {
            platform.TenantLicenses.Add(new TenantLicense
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductCode = ProductCodes.SilaMe,
                LicenseType = type,
                LicensedQuantity = quantity,
                ValidFrom = now,
                Status = LicenseStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    private async Task EnsureProductAsync(Guid tenantId, string product, DateTime now, CancellationToken cancellationToken)
    {
        if (!await platform.TenantProductEntitlements.AnyAsync(item => item.TenantId == tenantId && item.ProductCode == product, cancellationToken))
        {
            platform.TenantProductEntitlements.Add(new TenantProductEntitlement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductCode = product,
                Status = EntitlementStatus.ACTIVE,
                CreatedAt = now,
            });
        }
    }

    private async Task EnsureModuleAsync(Guid tenantId, string product, string module, DateTime now, CancellationToken cancellationToken)
    {
        if (!await platform.TenantModuleEntitlements.AnyAsync(item => item.TenantId == tenantId && item.ModuleCode == module, cancellationToken))
        {
            platform.TenantModuleEntitlements.Add(new TenantModuleEntitlement
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ProductCode = product,
                ModuleCode = module,
                Status = EntitlementStatus.ACTIVE,
                CreatedAt = now,
            });
        }
    }

    private async Task AuditAsync(Guid? actorId, string action, Guid tenantId, Guid? environmentId, CancellationToken cancellationToken)
    {
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorPlatformUserId = actorId,
            Action = action,
            TenantId = tenantId,
            TenantEnvironmentId = environmentId,
            CreatedAt = DateTime.UtcNow,
        });
        await Task.CompletedTask;
    }

    private static async Task CreateDatabaseIfMissingAsync(string operationalConnectionString, string databaseName, CancellationToken cancellationToken)
    {
        var normalized = DatabaseUrl.Normalize(operationalConnectionString);
        var builder = new NpgsqlConnectionStringBuilder(normalized) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
        exists.Parameters.AddWithValue("name", databaseName);
        var found = await exists.ExecuteScalarAsync(cancellationToken);
        if (found is not null)
        {
            return;
        }

        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName.Replace("\"", string.Empty)}\"", connection);
        await create.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ReplaceDatabase(string connectionString, string databaseName)
    {
        var normalized = DatabaseUrl.Normalize(connectionString);
        var builder = new NpgsqlConnectionStringBuilder(normalized) { Database = databaseName };
        return builder.ConnectionString;
    }
}

public interface IPlatformAuthService
{
    Task<(PlatformUser User, string RawToken)?> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<PlatformUser?> ValidateAsync(string? rawToken, CancellationToken cancellationToken);
    Task RevokeAsync(PlatformUser? user, string? rawToken, CancellationToken cancellationToken);
}

public sealed class PlatformAuthService(PlatformDbContext platform, IPasswordService passwords, IConfiguration configuration) : IPlatformAuthService
{
    public async Task<(PlatformUser User, string RawToken)?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await platform.PlatformUsers
            .Include(item => item.Roles).ThenInclude(item => item.Role).ThenInclude(item => item.Permissions).ThenInclude(item => item.Permission)
            .Include(item => item.TenantAssignments)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == request.Email.Trim().ToUpperInvariant(), cancellationToken);
        if (user is null || user.Status != PlatformUserStatus.ACTIVE)
        {
            return null;
        }

        var probe = new User { Id = user.Id, Email = user.Email, NormalizedEmail = user.NormalizedEmail, DisplayName = user.DisplayName, PasswordHash = user.PasswordHash };
        if (!passwords.VerifyPassword(probe, request.Password))
        {
            return null;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        platform.PlatformSessions.Add(new PlatformSession
        {
            Id = Guid.NewGuid(),
            PlatformUserId = user.Id,
            TokenHash = Hash(rawToken),
            CreatedAt = now,
            LastUsedAt = now,
            ExpiresAt = now.Add(CloudSessionCookie.Lifetime(configuration)),
        });
        user.LastLoginAt = now;
        await platform.SaveChangesAsync(cancellationToken);
        return (user, rawToken);
    }

    public async Task<PlatformUser?> ValidateAsync(string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = Hash(rawToken);
        var session = await platform.PlatformSessions
            .Include(item => item.User).ThenInclude(item => item.Roles).ThenInclude(item => item.Role).ThenInclude(item => item.Permissions).ThenInclude(item => item.Permission)
            .Include(item => item.User).ThenInclude(item => item.TenantAssignments)
            .SingleOrDefaultAsync(item => item.TokenHash == hash && item.RevokedAt == null, cancellationToken);
        if (session is null || session.ExpiresAt <= DateTime.UtcNow || session.User.Status != PlatformUserStatus.ACTIVE)
        {
            return null;
        }

        session.LastUsedAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.Add(CloudSessionCookie.Lifetime(configuration));
        await platform.SaveChangesAsync(cancellationToken);
        return session.User;
    }

    public async Task RevokeAsync(PlatformUser? user, string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return;
        }

        var hash = Hash(rawToken);
        var session = await platform.PlatformSessions.SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
        if (session is not null && session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            await platform.SaveChangesAsync(cancellationToken);
        }
    }

    private static string Hash(string rawToken) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}

public static class PlatformSessionCookie
{
    public const string Name = "sila_platform_session";
}

public static class PlatformContext
{
    public const string ItemKey = "SilaMe.PlatformUser";
}

public sealed class PlatformAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IPlatformAuthService auth, IConfiguration configuration)
    {
        if ((context.Request.Path.Value ?? string.Empty).StartsWith("/api/platform", StringComparison.OrdinalIgnoreCase))
        {
            var rawToken = context.Request.Cookies[PlatformSessionCookie.Name];
            var user = await auth.ValidateAsync(rawToken, context.RequestAborted);
            if (user is not null)
            {
                context.Items[PlatformContext.ItemKey] = user;
                if (!string.IsNullOrWhiteSpace(rawToken))
                {
                    context.Response.Cookies.Append(PlatformSessionCookie.Name, rawToken, CloudSessionCookie.Options(context.Request, configuration));
                }
            }
        }

        await next(context);
    }
}

public interface ITenantLaunchService
{
    Task<string> CreateAsync(
        PlatformUser actor,
        Guid tenantId,
        Guid environmentId,
        string customerOrigin,
        string? platformSessionToken,
        string? ipAddress,
        CancellationToken cancellationToken);
    Task<(Session Session, string RawToken)> ConsumeAsync(string code, string hostname, string? ipAddress, CancellationToken cancellationToken);
    Task EndSupportSessionAsync(Session session, string? ipAddress, CancellationToken cancellationToken);
}

public sealed class TenantLaunchService(
    PlatformDbContext platform,
    SilaMeDbContext tenantDb,
    ITenantContextAccessor tenants,
    ISessionService sessions) : ITenantLaunchService
{
    public const int LaunchTtlSeconds = 60;

    public async Task<string> CreateAsync(
        PlatformUser actor,
        Guid tenantId,
        Guid environmentId,
        string customerOrigin,
        string? platformSessionToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        try
        {
            var environment = await platform.TenantEnvironments.Include(item => item.Tenant)
                .SingleOrDefaultAsync(item => item.Id == environmentId && item.TenantId == tenantId, cancellationToken)
                ?? throw new TenantException("TENANT_NOT_FOUND", "The tenant environment was not found.", StatusCodes.Status404NotFound);
            if (environment.Tenant.Status == TenantStatus.SUSPENDED)
            {
                throw new TenantException("TENANT_SUSPENDED", "Users will not be able to access this organization's customer applications while the organization is suspended.");
            }

            if (actor.Status != PlatformUserStatus.ACTIVE)
            {
                throw new TenantException("PLATFORM_ACCESS_DENIED", "The platform user is not active.");
            }

            if (!environment.IsActive || environment.Status != TenantEnvironmentStatus.ACTIVE)
            {
                throw new TenantException("ENVIRONMENT_NOT_ACTIVE", "This tenant environment is not active.");
            }

            if (!PlatformAuthorization.CanLaunch(actor, tenantId, environmentId))
            {
                throw new TenantException("ENVIRONMENT_ACCESS_DENIED", "You are not assigned to launch this environment.");
            }

            var platformSession = await ResolvePlatformSessionAsync(actor.Id, platformSessionToken, cancellationToken)
                ?? throw new TenantException("SESSION_INVALID", "The platform session is no longer valid.");

            var role = PrimaryRole(actor);
            var permissions = AssignmentPermissions(actor, tenantId, environmentId);
            var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            platform.TenantLaunchAuthorizations.Add(new TenantLaunchAuthorization
            {
                Id = Guid.NewGuid(),
                PlatformUserId = actor.Id,
                PlatformSessionId = platformSession.Id,
                TenantId = tenantId,
                TenantEnvironmentId = environmentId,
                CodeHash = Hash(raw),
                PlatformRole = role,
                PermissionsCsv = permissions,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(LaunchTtlSeconds),
            });
            Audit(actor.Id, "PLATFORM_CUSTOMER_LAUNCH_REQUESTED", tenantId, environmentId, platformSession.Id, null, ipAddress);
            await platform.SaveChangesAsync(cancellationToken);
            return BuildRedirect(environment, customerOrigin, raw);
        }
        catch (TenantException)
        {
            Audit(actor.Id, "PLATFORM_CUSTOMER_LAUNCH_FAILED", tenantId, environmentId, null, null, ipAddress);
            await platform.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(Session Session, string RawToken)> ConsumeAsync(string code, string hostname, string? ipAddress, CancellationToken cancellationToken)
    {
        var context = tenants.Current ?? throw new TenantException("TENANT_NOT_FOUND", "Tenant context is missing.");
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new TenantException("TENANT_LAUNCH_INVALID", "The launch authorization is not valid.");
        }

        var hash = Hash(code);
        var authorization = await platform.TenantLaunchAuthorizations
            .SingleOrDefaultAsync(item => item.CodeHash == hash, cancellationToken);
        if (authorization is null)
        {
            Audit(null, "PLATFORM_CUSTOMER_LAUNCH_FAILED", context.TenantId, context.EnvironmentId, null, null, ipAddress);
            await platform.SaveChangesAsync(cancellationToken);
            throw new TenantException("TENANT_LAUNCH_INVALID", "The launch authorization is not valid.");
        }

        try
        {
            if (authorization.ConsumedAt is not null)
            {
                throw new TenantException("TENANT_LAUNCH_ALREADY_USED", "The launch authorization has already been used.");
            }

            if (authorization.ExpiresAt <= DateTime.UtcNow)
            {
                throw new TenantException("TENANT_LAUNCH_EXPIRED", "The launch authorization has expired.");
            }

            if (authorization.TenantId != context.TenantId)
            {
                throw new TenantException("TENANT_LAUNCH_INVALID", "The launch authorization does not match this tenant.");
            }

            if (authorization.TenantEnvironmentId != context.EnvironmentId)
            {
                throw new TenantException("ENVIRONMENT_ACCESS_DENIED", "The launch authorization does not match this environment.");
            }

            var actor = await platform.PlatformUsers
                .Include(item => item.Roles).ThenInclude(item => item.Role).ThenInclude(item => item.Permissions).ThenInclude(item => item.Permission)
                .Include(item => item.TenantAssignments)
                .SingleAsync(item => item.Id == authorization.PlatformUserId, cancellationToken);
            if (actor.Status != PlatformUserStatus.ACTIVE)
            {
                throw new TenantException("PLATFORM_ACCESS_DENIED", "The platform user is not active.");
            }

            var platformSession = authorization.PlatformSessionId is Guid sessionId
                ? await platform.PlatformSessions.SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken)
                : null;
            if (platformSession is null || platformSession.RevokedAt is not null || platformSession.ExpiresAt <= DateTime.UtcNow)
            {
                throw new TenantException("SESSION_INVALID", "The platform session is no longer valid.");
            }

            if (!PlatformAuthorization.CanLaunch(actor, authorization.TenantId, authorization.TenantEnvironmentId))
            {
                throw new TenantException("ENVIRONMENT_ACCESS_DENIED", "You are not assigned to launch this environment.");
            }

            var consumed = await platform.TenantLaunchAuthorizations
                .Where(item => item.Id == authorization.Id && item.ConsumedAt == null)
                .ExecuteUpdateAsync(item => item
                    .SetProperty(row => row.ConsumedAt, DateTime.UtcNow)
                    .SetProperty(row => row.ConsumedFromHost, hostname), cancellationToken);
            if (consumed != 1)
            {
                throw new TenantException("TENANT_LAUNCH_ALREADY_USED", "The launch authorization has already been used.");
            }

            var created = await sessions.CreateSupportAsync(
                actor.Id,
                actor.DisplayName,
                actor.Email,
                authorization.PlatformRole ?? PrimaryRole(actor),
                context.EnvironmentId,
                platformSession.Id,
                authorization.PermissionsCsv ?? AssignmentPermissions(actor, authorization.TenantId, authorization.TenantEnvironmentId),
                cancellationToken);
            authorization.CustomerSessionId = created.Session.Id;
            Audit(actor.Id, "PLATFORM_CUSTOMER_LAUNCH_SUCCEEDED", context.TenantId, context.EnvironmentId, platformSession.Id, created.Session.Id, ipAddress);
            tenantDb.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                EventType = "PLATFORM_CUSTOMER_LAUNCH_SUCCEEDED",
                EntityType = "SESSION",
                EntityId = created.Session.Id,
                MetadataJson = $"{{\"actorType\":\"PLATFORM_USER\",\"platformUserId\":\"{actor.Id:D}\",\"platformRole\":\"{authorization.PlatformRole}\"}}",
                Result = "SUCCEEDED",
                CreatedAt = DateTime.UtcNow,
            });
            await platform.SaveChangesAsync(cancellationToken);
            await tenantDb.SaveChangesAsync(cancellationToken);
            return (created.Session, created.RawToken);
        }
        catch (TenantException)
        {
            Audit(authorization.PlatformUserId, "PLATFORM_CUSTOMER_LAUNCH_FAILED", context.TenantId, context.EnvironmentId, authorization.PlatformSessionId, null, ipAddress);
            await platform.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task EndSupportSessionAsync(Session session, string? ipAddress, CancellationToken cancellationToken)
    {
        await sessions.RevokeAsync(session, cancellationToken);
        Audit(session.PlatformUserId, "PLATFORM_CUSTOMER_SESSION_ENDED", session.TenantId, session.TenantEnvironmentId, session.SourcePlatformSessionId, session.Id, ipAddress);
        await platform.SaveChangesAsync(cancellationToken);
    }

    private async Task<PlatformSession?> ResolvePlatformSessionAsync(Guid platformUserId, string? rawToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = HashToken(rawToken);
        return await platform.PlatformSessions.SingleOrDefaultAsync(
            item => item.TokenHash == hash && item.PlatformUserId == platformUserId && item.RevokedAt == null && item.ExpiresAt > DateTime.UtcNow,
            cancellationToken);
    }

    private void Audit(Guid? actorId, string action, Guid? tenantId, Guid? environmentId, Guid? platformSessionId, Guid? customerSessionId, string? ip)
    {
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorPlatformUserId = actorId,
            Action = action,
            TenantId = tenantId,
            TenantEnvironmentId = environmentId,
            TargetType = "SESSION",
            TargetId = customerSessionId?.ToString("D"),
            CorrelationId = platformSessionId?.ToString("D"),
            MetadataJson = ip is null ? null : $"{{\"ip\":\"{ip}\"}}",
            CreatedAt = DateTime.UtcNow,
        });
    }

    private static string BuildRedirect(TenantEnvironment environment, string customerOrigin, string raw)
    {
        var origin = string.IsNullOrWhiteSpace(environment.BaseUrl) ? customerOrigin.TrimEnd('/') : environment.BaseUrl.TrimEnd('/');
        var slug = environment.RouteSlug;
        if (!string.IsNullOrWhiteSpace(slug) && !origin.EndsWith("/" + slug, StringComparison.OrdinalIgnoreCase))
        {
            origin = $"{origin}/{slug.Trim('/')}";
        }

        return $"{origin}/platform-launch?code={raw}";
    }

    private static string PrimaryRole(PlatformUser actor) =>
        actor.Roles.Select(item => item.Role.Key).FirstOrDefault(key => key.StartsWith("PLATFORM_", StringComparison.Ordinal) || key.StartsWith("CUSTOMER_SUPPORT", StringComparison.Ordinal))
        ?? actor.Roles.Select(item => item.Role.Key).FirstOrDefault()
        ?? "PLATFORM_USER";

    private static string AssignmentPermissions(PlatformUser actor, Guid tenantId, Guid environmentId)
    {
        if (PlatformAuthorization.IsSuperAdmin(actor))
        {
            return string.Join(',', PermissionKeys.All.Select(item => item.Key));
        }

        var now = DateTime.UtcNow;
        var assignment = actor.TenantAssignments.FirstOrDefault(item =>
            item.Status == TenantAssignmentStatus.ACTIVE
            && item.TenantId == tenantId
            && (item.TenantEnvironmentId == null || item.TenantEnvironmentId == environmentId)
            && item.ValidFrom <= now
            && (item.ValidUntil == null || item.ValidUntil > now));
        return assignment?.PermissionsCsv ?? string.Empty;
    }

    private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
    private static string HashToken(string raw) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
}

public static class PlatformAuthorization
{
    public static bool IsSuperAdmin(PlatformUser user) =>
        user.Roles.Any(item => item.Role.Key == "PLATFORM_SUPER_ADMIN");

    public static bool HasPermission(PlatformUser user, string permission) =>
        IsSuperAdmin(user) || user.Roles.SelectMany(item => item.Role.Permissions).Any(item => item.Permission.Key == permission);

    public static bool CanAccessTenant(PlatformUser user, Guid tenantId, Guid? environmentId = null)
    {
        if (IsSuperAdmin(user))
        {
            return true;
        }

        var now = DateTime.UtcNow;
        return user.TenantAssignments.Any(item =>
            item.Status == TenantAssignmentStatus.ACTIVE
            && item.TenantId == tenantId
            && (item.TenantEnvironmentId == null || environmentId == null || item.TenantEnvironmentId == environmentId)
            && item.ValidFrom <= now
            && (item.ValidUntil == null || item.ValidUntil > now));
    }

    public static bool CanLaunch(PlatformUser user, Guid tenantId, Guid environmentId) =>
        HasPermission(user, PlatformPermissionKeys.TenantsLaunch)
        && CanAccessTenant(user, tenantId, environmentId);
}
