namespace SilaMe.Api.Models;

public enum TenantStatus
{
    PROVISIONING,
    ACTIVE,
    SUSPENDED,
    ARCHIVED,
}

public enum TenantEnvironmentType
{
    DEVELOPMENT,
    TEST,
    PRODUCTION,
}

public enum TenantEnvironmentStatus
{
    PROVISIONING,
    ACTIVE,
    MAINTENANCE,
    SUSPENDED,
    FAILED,
}

public enum TenantDomainType
{
    SILA_SUBDOMAIN,
    CUSTOM,
    LOOPBACK,
}

public enum PlatformUserStatus
{
    ACTIVE,
    INACTIVE,
}

public enum TenantAssignmentStatus
{
    ACTIVE,
    INACTIVE,
}

public enum LicenseStatus
{
    ACTIVE,
    EXPIRED,
    SUSPENDED,
}

public enum EntitlementStatus
{
    ACTIVE,
    DISABLED,
}

public enum ProvisioningCheckpoint
{
    TENANT_CREATED,
    DATABASE_CREATED,
    MIGRATIONS_APPLIED,
    DOMAIN_REGISTERED,
    ADMIN_CREATED,
    ENTITLEMENTS_CREATED,
    READY,
    PROVISIONING_FAILED,
}

public sealed class Tenant
{
    public Guid Id { get; set; }
    public required string TenantCode { get; set; }
    public required string NormalizedTenantCode { get; set; }
    public required string CustomerName { get; set; }
    public string? LegalName { get; set; }
    public string? CountryCode { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? BillingContactEmail { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string DefaultTimeZone { get; set; } = "UTC";
    public string DefaultCurrency { get; set; } = "AED";
    public string DefaultLanguage { get; set; } = "en";
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.PROVISIONING;
    public string SupportLevel { get; set; } = "STANDARD";
    public int MaxCloudUsers { get; set; } = 25;
    public int MaxMobileUsers { get; set; } = 100;
    public int LicenseCount { get; set; }
    public int TotalUsers { get; set; }
    public string? LogoFileName { get; set; }
    public string? LoginWelcomeText { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByPlatformUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedByPlatformUserId { get; set; }
    public ICollection<TenantEnvironment> Environments { get; set; } = [];
    public ICollection<TenantDomain> Domains { get; set; } = [];
    public ICollection<TenantLicense> Licenses { get; set; } = [];
    public ICollection<TenantProductEntitlement> ProductEntitlements { get; set; } = [];
    public ICollection<TenantModuleEntitlement> ModuleEntitlements { get; set; } = [];
}

public sealed class TenantEnvironment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public TenantEnvironmentType EnvironmentType { get; set; }
    public required string DisplayName { get; set; }
    public TenantEnvironmentStatus Status { get; set; } = TenantEnvironmentStatus.PROVISIONING;
    public required string BaseUrl { get; set; }
    public string? RouteSlug { get; set; }
    public required string DatabaseSecretReference { get; set; }
    public string? StorageConfigurationReference { get; set; }
    public string? DeploymentReference { get; set; }
    public string DataRegion { get; set; } = "LOCAL";
    public bool IsActive { get; set; }
    public ProvisioningCheckpoint ProvisioningCheckpoint { get; set; } = ProvisioningCheckpoint.TENANT_CREATED;
    public string? LastAppliedMigration { get; set; }
    public string? ProvisioningError { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public ICollection<TenantDomain> Domains { get; set; } = [];
}

public sealed class TenantDomain
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TenantEnvironmentId { get; set; }
    public required string Hostname { get; set; }
    public TenantDomainType DomainType { get; set; } = TenantDomainType.SILA_SUBDOMAIN;
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public TenantEnvironment Environment { get; set; } = null!;
}

public sealed class PlatformUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string PasswordHash { get; set; }
    public PlatformUserStatus Status { get; set; } = PlatformUserStatus.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<PlatformUserRole> Roles { get; set; } = [];
    public ICollection<PlatformUserTenantAssignment> TenantAssignments { get; set; } = [];
    public ICollection<PlatformSession> Sessions { get; set; } = [];
}

public sealed class PlatformRole
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<PlatformRolePermission> Permissions { get; set; } = [];
    public ICollection<PlatformUserRole> Users { get; set; } = [];
}

public sealed class PlatformPermission
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<PlatformRolePermission> Roles { get; set; } = [];
}

public sealed class PlatformRolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public PlatformRole Role { get; set; } = null!;
    public PlatformPermission Permission { get; set; } = null!;
}

public sealed class PlatformUserRole
{
    public Guid Id { get; set; }
    public Guid PlatformUserId { get; set; }
    public Guid PlatformRoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public PlatformUser User { get; set; } = null!;
    public PlatformRole Role { get; set; } = null!;
}

public sealed class PlatformUserTenantAssignment
{
    public Guid Id { get; set; }
    public Guid PlatformUserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? TenantEnvironmentId { get; set; }
    public string PermissionsCsv { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public TenantAssignmentStatus Status { get; set; } = TenantAssignmentStatus.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public PlatformUser User { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public TenantEnvironment? Environment { get; set; }
}

public sealed class TenantLicense
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ProductCode { get; set; } = "SILA_ME";
    public string LicenseType { get; set; } = "CLOUD_USERS";
    public int LicensedQuantity { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
}

public sealed class TenantProductEntitlement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string ProductCode { get; set; }
    public EntitlementStatus Status { get; set; } = EntitlementStatus.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
}

public sealed class TenantModuleEntitlement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string ProductCode { get; set; }
    public required string ModuleCode { get; set; }
    public EntitlementStatus Status { get; set; } = EntitlementStatus.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public Tenant Tenant { get; set; } = null!;
}

public sealed class PlatformSession
{
    public Guid Id { get; set; }
    public Guid PlatformUserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public PlatformUser User { get; set; } = null!;
}

public sealed class TenantLaunchAuthorization
{
    public Guid Id { get; set; }
    public Guid PlatformUserId { get; set; }
    public Guid? PlatformSessionId { get; set; }
    public Guid TenantId { get; set; }
    public Guid TenantEnvironmentId { get; set; }
    public required string CodeHash { get; set; }
    public string? PlatformRole { get; set; }
    public string? PermissionsCsv { get; set; }
    public Guid? CustomerSessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? ConsumedFromHost { get; set; }
}

public sealed class PlatformAuditEvent
{
    public Guid Id { get; set; }
    public Guid? ActorPlatformUserId { get; set; }
    public required string Action { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? TenantEnvironmentId { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
