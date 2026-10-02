using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Tenancy;

public static class PlatformBootstrap
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task RunAsync(IServiceProvider services, string operationalConnectionString, CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await RunUnsafeAsync(services, operationalConnectionString, cancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task RunUnsafeAsync(IServiceProvider services, string operationalConnectionString, CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await platform.Database.EnsureCreatedAsync(cancellationToken);
        await EnsurePlatformSchemaAsync(platform, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var definition in PlatformPermissionKeys.All.Select(key => (Key: key, Name: key.Replace('.', ' '))))
        {
            if (!await platform.PlatformPermissions.AnyAsync(item => item.Key == definition.Key, cancellationToken))
            {
                platform.PlatformPermissions.Add(new PlatformPermission
                {
                    Id = Guid.NewGuid(),
                    Key = definition.Key,
                    Name = definition.Name,
                });
            }
        }

        await platform.SaveChangesAsync(cancellationToken);
        var permissions = await platform.PlatformPermissions.ToListAsync(cancellationToken);

        await EnsureRoleAsync(platform, "PLATFORM_SUPER_ADMIN", "Platform Super Admin", permissions.Select(item => item.Id).ToArray(), now, cancellationToken);
        await EnsureRoleAsync(platform, "SILA_ADMIN", "SILA Admin", permissions.Where(item => item.Key is not PlatformPermissionKeys.TenantsProvision).Select(item => item.Id).ToArray(), now, cancellationToken);
        await EnsureRoleAsync(platform, "PLATFORM_ADMIN", "Platform Admin", permissions.Where(item => item.Key is PlatformPermissionKeys.TenantsRead or PlatformPermissionKeys.AuditRead).Select(item => item.Id).ToArray(), now, cancellationToken);
        await EnsureRoleAsync(platform, "CUSTOMER_SUPPORT_ADMIN", "Customer Support Admin", permissions.Where(item => item.Key is PlatformPermissionKeys.TenantsRead or PlatformPermissionKeys.TenantsLaunch or PlatformPermissionKeys.SupportAssign or PlatformPermissionKeys.ViewTenant or PlatformPermissionKeys.LaunchTenant or PlatformPermissionKeys.ViewLogs).Select(item => item.Id).ToArray(), now, cancellationToken);
        await EnsureRoleAsync(platform, "CUSTOMER_SUPPORT_CONSULTANT", "Customer Support Consultant", permissions.Where(item => item.Key is PlatformPermissionKeys.TenantsRead or PlatformPermissionKeys.TenantsLaunch or PlatformPermissionKeys.ViewTenant or PlatformPermissionKeys.LaunchTenant).Select(item => item.Id).ToArray(), now, cancellationToken);
        await platform.SaveChangesAsync(cancellationToken);

        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var super = await platform.PlatformRoles.SingleAsync(item => item.Key == "PLATFORM_SUPER_ADMIN", cancellationToken);
        var platformAdminPassword = Environment.GetEnvironmentVariable("SILA_ME_PLATFORM_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(platformAdminPassword))
        {
            await EnsurePlatformSuperAdminAsync(
                platform,
                super,
                passwords,
                email: "platformadmin@silame.local",
                displayName: "SILA Platform Super Admin",
                passwordHash: null,
                password: platformAdminPassword,
                now,
                cancellationToken);
        }

        await EnsureBalaPlatformSuperAdminAsync(
            platform,
            super,
            passwords,
            operationalConnectionString,
            now,
            cancellationToken);

        await platform.SaveChangesAsync(cancellationToken);
        var provisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
        await provisioning.RegisterDevelopmentTenantAsync(operationalConnectionString, cancellationToken);
        await FiveCustomerBootstrap.EnsureAsync(scope.ServiceProvider, operationalConnectionString, cancellationToken);
    }

    private static async Task EnsurePlatformSchemaAsync(PlatformDbContext platform, CancellationToken cancellationToken)
    {
        var statements = new[]
        {
            """ALTER TABLE platform_users ADD COLUMN IF NOT EXISTS "FirstName" character varying(80);""",
            """ALTER TABLE platform_users ADD COLUMN IF NOT EXISTS "LastName" character varying(80);""",
            """ALTER TABLE platform_tenants ADD COLUMN IF NOT EXISTS "NormalizedTenantCode" character varying(64);""",
            """ALTER TABLE platform_tenants ADD COLUMN IF NOT EXISTS "LicenseCount" integer NOT NULL DEFAULT 0;""",
            """ALTER TABLE platform_tenants ADD COLUMN IF NOT EXISTS "TotalUsers" integer NOT NULL DEFAULT 0;""",
            """ALTER TABLE platform_tenant_environments ADD COLUMN IF NOT EXISTS "RouteSlug" character varying(80);""",
            """ALTER TABLE platform_tenant_environments ADD COLUMN IF NOT EXISTS "LastAppliedMigration" character varying(200);""",
            """ALTER TABLE platform_tenant_launch_authorizations ADD COLUMN IF NOT EXISTS "PlatformSessionId" uuid;""",
            """ALTER TABLE platform_tenant_launch_authorizations ADD COLUMN IF NOT EXISTS "PlatformRole" character varying(80);""",
            """ALTER TABLE platform_tenant_launch_authorizations ADD COLUMN IF NOT EXISTS "PermissionsCsv" character varying(4000);""",
            """ALTER TABLE platform_tenant_launch_authorizations ADD COLUMN IF NOT EXISTS "CustomerSessionId" uuid;""",
            """UPDATE platform_tenants SET "NormalizedTenantCode" = UPPER("TenantCode") WHERE "NormalizedTenantCode" IS NULL OR "NormalizedTenantCode" = '';""",
            """CREATE UNIQUE INDEX IF NOT EXISTS "IX_platform_tenants_NormalizedTenantCode" ON platform_tenants ("NormalizedTenantCode");""",
            """CREATE UNIQUE INDEX IF NOT EXISTS "IX_platform_tenant_environments_RouteSlug" ON platform_tenant_environments ("RouteSlug") WHERE "RouteSlug" IS NOT NULL;""",
        };
        foreach (var sql in statements)
        {
            await platform.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }

    private static async Task EnsureBalaPlatformSuperAdminAsync(
        PlatformDbContext platform,
        PlatformRole super,
        IPasswordService passwords,
        string operationalConnectionString,
        DateTime now,
        CancellationToken cancellationToken)
    {
        const string email = "bala@chervic.in";
        string? inheritedHash = null;
        var displayName = "Bala";
        string? firstName = "Bala";
        string? lastName = null;

        var tenantOptions = new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(DatabaseUrl.Normalize(operationalConnectionString))
            .Options;
        await using (var tenantDb = new SilaMeDbContext(tenantOptions))
        {
            var tenantUser = await tenantDb.Users.AsNoTracking()
                .SingleOrDefaultAsync(item => item.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);
            if (tenantUser is not null)
            {
                inheritedHash = tenantUser.PasswordHash;
                displayName = string.IsNullOrWhiteSpace(tenantUser.DisplayName) ? displayName : tenantUser.DisplayName;
                firstName = tenantUser.FirstName ?? firstName;
                lastName = tenantUser.LastName;
            }
        }

        var fallbackPassword = Environment.GetEnvironmentVariable("SILA_ME_ADMIN_PASSWORD")
            ?? Environment.GetEnvironmentVariable("SILA_ME_PLATFORM_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(inheritedHash) && string.IsNullOrWhiteSpace(fallbackPassword))
        {
            return;
        }

        await EnsurePlatformSuperAdminAsync(
            platform,
            super,
            passwords,
            email,
            displayName,
            inheritedHash,
            fallbackPassword,
            now,
            cancellationToken,
            firstName,
            lastName);
    }

    private static async Task EnsurePlatformSuperAdminAsync(
        PlatformDbContext platform,
        PlatformRole super,
        IPasswordService passwords,
        string email,
        string displayName,
        string? passwordHash,
        string? password,
        DateTime now,
        CancellationToken cancellationToken,
        string? firstName = null,
        string? lastName = null)
    {
        var user = await platform.PlatformUsers.Include(item => item.Roles)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == email.ToUpperInvariant(), cancellationToken);
        if (user is null)
        {
            user = new PlatformUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = displayName,
                FirstName = firstName,
                LastName = lastName,
                PasswordHash = string.Empty,
                Status = PlatformUserStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now,
            };
            if (!string.IsNullOrWhiteSpace(passwordHash))
            {
                user.PasswordHash = passwordHash;
            }
            else
            {
                var probe = new User { Id = user.Id, Email = user.Email, NormalizedEmail = user.NormalizedEmail, DisplayName = user.DisplayName, PasswordHash = string.Empty };
                user.PasswordHash = passwords.HashPassword(probe, password!);
            }

            platform.PlatformUsers.Add(user);
        }
        else if (user.Status != PlatformUserStatus.ACTIVE)
        {
            user.Status = PlatformUserStatus.ACTIVE;
            user.UpdatedAt = now;
        }

        if (user.Roles.All(item => item.PlatformRoleId != super.Id))
        {
            platform.PlatformUserRoles.Add(new PlatformUserRole
            {
                Id = Guid.NewGuid(),
                PlatformUserId = user.Id,
                PlatformRoleId = super.Id,
                CreatedAt = now,
            });
        }
    }

    private static async Task EnsureRoleAsync(
        PlatformDbContext platform,
        string key,
        string name,
        Guid[] permissionIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var role = await platform.PlatformRoles.Include(item => item.Permissions)
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (role is null)
        {
            role = new PlatformRole
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                IsSystem = true,
                CreatedAt = now,
            };
            platform.PlatformRoles.Add(role);
        }

        foreach (var permissionId in permissionIds)
        {
            if (role.Permissions.All(item => item.PermissionId != permissionId))
            {
                role.Permissions.Add(new PlatformRolePermission { RoleId = role.Id, PermissionId = permissionId });
            }
        }
    }
}
