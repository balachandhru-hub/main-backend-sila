using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Data;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantEnvironment> TenantEnvironments => Set<TenantEnvironment>();
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<PlatformPermission> PlatformPermissions => Set<PlatformPermission>();
    public DbSet<PlatformRolePermission> PlatformRolePermissions => Set<PlatformRolePermission>();
    public DbSet<PlatformUserRole> PlatformUserRoles => Set<PlatformUserRole>();
    public DbSet<PlatformUserTenantAssignment> PlatformUserTenantAssignments => Set<PlatformUserTenantAssignment>();
    public DbSet<TenantLicense> TenantLicenses => Set<TenantLicense>();
    public DbSet<TenantProductEntitlement> TenantProductEntitlements => Set<TenantProductEntitlement>();
    public DbSet<TenantModuleEntitlement> TenantModuleEntitlements => Set<TenantModuleEntitlement>();
    public DbSet<PlatformSession> PlatformSessions => Set<PlatformSession>();
    public DbSet<TenantLaunchAuthorization> TenantLaunchAuthorizations => Set<TenantLaunchAuthorization>();
    public DbSet<PlatformAuditEvent> PlatformAuditEvents => Set<PlatformAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("platform_tenants");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TenantCode).HasMaxLength(64).IsRequired();
            entity.Property(item => item.NormalizedTenantCode).HasMaxLength(64).IsRequired();
            entity.Property(item => item.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.LegalName).HasMaxLength(200);
            entity.Property(item => item.CountryCode).HasMaxLength(8);
            entity.Property(item => item.TaxRegistrationNumber).HasMaxLength(64);
            entity.Property(item => item.PrimaryContactName).HasMaxLength(160);
            entity.Property(item => item.PrimaryContactEmail).HasMaxLength(320);
            entity.Property(item => item.PrimaryContactPhone).HasMaxLength(40);
            entity.Property(item => item.BillingContactEmail).HasMaxLength(320);
            entity.Property(item => item.DefaultTimeZone).HasMaxLength(64);
            entity.Property(item => item.DefaultCurrency).HasMaxLength(8);
            entity.Property(item => item.DefaultLanguage).HasMaxLength(16);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(item => item.SupportLevel).HasMaxLength(32);
            entity.HasIndex(item => item.TenantCode).IsUnique();
            entity.HasIndex(item => item.NormalizedTenantCode).IsUnique();
        });

        modelBuilder.Entity<TenantEnvironment>(entity =>
        {
            entity.ToTable("platform_tenant_environments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EnvironmentType).HasConversion<string>().HasMaxLength(32);
            entity.Property(item => item.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(item => item.BaseUrl).HasMaxLength(300).IsRequired();
            entity.Property(item => item.RouteSlug).HasMaxLength(80);
            entity.Property(item => item.DatabaseSecretReference).HasMaxLength(200).IsRequired();
            entity.HasIndex(item => item.RouteSlug).IsUnique().HasFilter("\"RouteSlug\" IS NOT NULL");
            entity.Property(item => item.StorageConfigurationReference).HasMaxLength(200);
            entity.Property(item => item.DataRegion).HasMaxLength(32);
            entity.Property(item => item.ProvisioningCheckpoint).HasConversion<string>().HasMaxLength(40);
            entity.Property(item => item.LastAppliedMigration).HasMaxLength(200);
            entity.HasIndex(item => new { item.TenantId, item.EnvironmentType }).IsUnique();
            entity.HasOne(item => item.Tenant).WithMany(tenant => tenant.Environments)
                .HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantDomain>(entity =>
        {
            entity.ToTable("platform_tenant_domains");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Hostname).HasMaxLength(253).IsRequired();
            entity.Property(item => item.DomainType).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(item => item.Hostname).IsUnique();
            entity.HasOne(item => item.Tenant).WithMany(tenant => tenant.Domains)
                .HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Environment).WithMany(environment => environment.Domains)
                .HasForeignKey(item => item.TenantEnvironmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformUser>(entity =>
        {
            entity.ToTable("platform_users");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
            entity.Property(item => item.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(item => item.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(item => item.FirstName).HasMaxLength(80);
            entity.Property(item => item.LastName).HasMaxLength(80);
            entity.Property(item => item.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(item => item.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<PlatformRole>(entity =>
        {
            entity.ToTable("platform_roles");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Key).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(item => item.Key).IsUnique();
        });

        modelBuilder.Entity<PlatformPermission>(entity =>
        {
            entity.ToTable("platform_permissions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Key).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.HasIndex(item => item.Key).IsUnique();
        });

        modelBuilder.Entity<PlatformRolePermission>(entity =>
        {
            entity.ToTable("platform_role_permissions");
            entity.HasKey(item => new { item.RoleId, item.PermissionId });
            entity.HasOne(item => item.Role).WithMany(role => role.Permissions)
                .HasForeignKey(item => item.RoleId);
            entity.HasOne(item => item.Permission).WithMany(permission => permission.Roles)
                .HasForeignKey(item => item.PermissionId);
        });

        modelBuilder.Entity<PlatformUserRole>(entity =>
        {
            entity.ToTable("platform_user_roles");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.PlatformUserId, item.PlatformRoleId }).IsUnique();
            entity.HasOne(item => item.User).WithMany(user => user.Roles)
                .HasForeignKey(item => item.PlatformUserId);
            entity.HasOne(item => item.Role).WithMany(role => role.Users)
                .HasForeignKey(item => item.PlatformRoleId);
        });

        modelBuilder.Entity<PlatformUserTenantAssignment>(entity =>
        {
            entity.ToTable("platform_user_tenant_assignments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PermissionsCsv).HasMaxLength(1000);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(item => item.User).WithMany(user => user.TenantAssignments)
                .HasForeignKey(item => item.PlatformUserId);
            entity.HasOne(item => item.Tenant).WithMany()
                .HasForeignKey(item => item.TenantId);
            entity.HasOne(item => item.Environment).WithMany()
                .HasForeignKey(item => item.TenantEnvironmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantLicense>(entity =>
        {
            entity.ToTable("platform_tenant_licenses");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductCode).HasMaxLength(40);
            entity.Property(item => item.LicenseType).HasMaxLength(40);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasOne(item => item.Tenant).WithMany(tenant => tenant.Licenses)
                .HasForeignKey(item => item.TenantId);
        });

        modelBuilder.Entity<TenantProductEntitlement>(entity =>
        {
            entity.ToTable("platform_tenant_product_entitlements");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductCode).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(item => new { item.TenantId, item.ProductCode }).IsUnique();
            entity.HasOne(item => item.Tenant).WithMany(tenant => tenant.ProductEntitlements)
                .HasForeignKey(item => item.TenantId);
        });

        modelBuilder.Entity<TenantModuleEntitlement>(entity =>
        {
            entity.ToTable("platform_tenant_module_entitlements");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductCode).HasMaxLength(40).IsRequired();
            entity.Property(item => item.ModuleCode).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(item => new { item.TenantId, item.ProductCode, item.ModuleCode }).IsUnique();
            entity.HasOne(item => item.Tenant).WithMany(tenant => tenant.ModuleEntitlements)
                .HasForeignKey(item => item.TenantId);
        });

        modelBuilder.Entity<PlatformSession>(entity =>
        {
            entity.ToTable("platform_sessions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(item => item.TokenHash).IsUnique();
            entity.HasOne(item => item.User).WithMany(user => user.Sessions)
                .HasForeignKey(item => item.PlatformUserId);
        });

        modelBuilder.Entity<TenantLaunchAuthorization>(entity =>
        {
            entity.ToTable("platform_tenant_launch_authorizations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.PlatformRole).HasMaxLength(80);
            entity.Property(item => item.PermissionsCsv).HasMaxLength(4000);
            entity.HasIndex(item => item.CodeHash).IsUnique();
        });

        modelBuilder.Entity<PlatformAuditEvent>(entity =>
        {
            entity.ToTable("platform_audit_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Action).HasMaxLength(80).IsRequired();
            entity.Property(item => item.TargetType).HasMaxLength(80);
            entity.Property(item => item.TargetId).HasMaxLength(80);
            entity.Property(item => item.CorrelationId).HasMaxLength(80);
        });
    }
}
