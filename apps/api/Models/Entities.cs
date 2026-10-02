namespace SilaMe.Api.Models;

public enum ApplicationKind
{
    CLOUD,
    MOBILE,
}

public enum AuthorizationOverrideType
{
    GRANT,
    DENY,
}

public enum DocumentStorageProvider
{
    MICROSOFT,
    GOOGLE,
    OTHER,
    NONE,
}

public enum StorageConnectionStatus
{
    NOT_CONNECTED,
    PENDING,
    AUTHENTICATION_REQUIRED,
    AUTHENTICATING,
    AUTHENTICATED,
    SITE_SELECTION_REQUIRED,
    LIBRARY_SELECTION_REQUIRED,
    FOLDER_SELECTION_REQUIRED,
    VALIDATING,
    CONNECTED,
    CONSENT_REQUIRED,
    EXPIRED,
    VALIDATION_FAILED,
    DISCONNECTED,
    FAILED,
}

public enum StorageAssignmentStatus
{
    NOT_CONFIGURED,
    PENDING_VALIDATION,
    VALIDATED,
    DISCONNECTED,
}

public enum StorageDestinationStatus
{
    ACTIVE,
    INACTIVE,
    VALIDATION_REQUIRED,
}

public enum DocumentTransferStatus
{
    PENDING,
    PROCESSING,
    COMPLETED,
    RETRY_PENDING,
    FAILED,
    FAILED_AUTHENTICATION,
    SKIPPED,
}

public enum StatusKind
{
    ACTIVE,
    INACTIVE,
}

public enum OrganizationKind
{
    CUSTOMER,
    HOSPITALITY_GROUP,
}

public enum OrganizationUnitKind
{
    PROPERTY,
    HOTEL,
    OUTLET,
    KITCHEN,
    STORE,
    STORAGE_LOCATION,
}

public sealed class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MobileNumber { get; set; }
    public bool MustChangePassword { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<UserApplicationAccess> ApplicationAccess { get; set; } = [];
    public ICollection<UserOrganizationMembership> OrganizationMemberships { get; set; } = [];
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
    public ICollection<UserAuthorizationOverride> AuthorizationOverrides { get; set; } = [];
    public ICollection<UserDocumentStorageAssignment> DocumentStorageAssignments { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
}

public sealed class UserApplicationAccess
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationKind Application { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
}

public sealed class Session
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public ApplicationKind Application { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsSupportSession { get; set; }
    public Guid? SupportSessionId { get; set; }
    public Guid? PlatformUserId { get; set; }
    public string? PlatformUserDisplayName { get; set; }
    public string? PlatformUserEmail { get; set; }
    public string? PlatformRole { get; set; }
    public string ActorType { get; set; } = "CUSTOMER_USER";
    public Guid? SourcePlatformSessionId { get; set; }
    public string? DelegatedPermissionsCsv { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? TenantEnvironmentId { get; set; }
    public User? User { get; set; }
}

public sealed class Organization
{
    public Guid Id { get; set; }
    public Guid? ParentOrganizationId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public OrganizationKind Kind { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public string? CustomerLogoFileName { get; set; }
    public string? CustomerLogoContentType { get; set; }
    public DateTime? CustomerLogoUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization? ParentOrganization { get; set; }
    public ICollection<Organization> ChildOrganizations { get; set; } = [];
    public ICollection<OrganizationUnit> Units { get; set; } = [];
    public ICollection<UserOrganizationMembership> Memberships { get; set; } = [];
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
}

public sealed class OrganizationUnit
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ParentUnitId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public OrganizationUnitKind Kind { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? ParentUnit { get; set; }
    public ICollection<OrganizationUnit> ChildUnits { get; set; } = [];
    public ICollection<UserOrganizationMembership> Memberships { get; set; } = [];
    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
}

public sealed class Role
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public string ApplicationScope { get; set; } = "BOTH";
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<RolePermission> Permissions { get; set; } = [];
    public ICollection<UserRoleAssignment> UserAssignments { get; set; } = [];
}

public sealed class Permission
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string Module { get; set; } = "Administration";
    public string? SubModule { get; set; }
    public string ApplicationScope { get; set; } = "BOTH";
    public string RiskLevel { get; set; } = "LOW";
    public bool IsSystemAuthorization { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<RolePermission> Roles { get; set; } = [];
}

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

public sealed class UserOrganizationMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OrganizationUnit { get; set; }
}

public sealed class UserRoleAssignment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OrganizationUnit { get; set; }
}

public sealed class UserAuthorizationOverride
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AuthorizationId { get; set; }
    public AuthorizationOverrideType OverrideType { get; set; }
    public string? Reason { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
    public Permission Authorization { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}

public sealed class DocumentStorageConnection
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public DocumentStorageProvider Provider { get; set; }
    public required string Name { get; set; }
    public StorageConnectionStatus ConnectionStatus { get; set; } = StorageConnectionStatus.NOT_CONNECTED;
    public string? TenantIdentifier { get; set; }
    public string? SiteIdentifier { get; set; }
    public string? DriveIdentifier { get; set; }
    public string? FolderIdentifier { get; set; }
    public string? FolderPath { get; set; }
    public string? DisplayUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? DriveName { get; set; }
    public string? CredentialReference { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public DateTime? LastTestedAt { get; set; }
    public string? LastTestStatus { get; set; }
    public Guid? ValidatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public User? ValidatedByUser { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<UserDocumentStorageAssignment> Assignments { get; set; } = [];
    public ICollection<DocumentStorageDestination> Destinations { get; set; } = [];
}

public sealed class DocumentStorageDestination
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid StorageConnectionId { get; set; }
    public DocumentStorageProvider Provider { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.INVOICE;
    public string? SiteIdentifier { get; set; }
    public string? DriveIdentifier { get; set; }
    public string? FolderIdentifier { get; set; }
    public required string FolderPath { get; set; }
    public string? DisplayUrl { get; set; }
    public bool ExternalTransferEnabled { get; set; }
    public StorageDestinationStatus Status { get; set; } = StorageDestinationStatus.VALIDATION_REQUIRED;
    public DateTime? ValidatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OperatingUnit { get; set; }
    public DocumentStorageConnection StorageConnection { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<UserDocumentStorageAssignment> UserAssignments { get; set; } = [];
    public ICollection<DocumentTransferJob> TransferJobs { get; set; } = [];
}

public sealed class UserDocumentStorageAssignment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DocumentStorageDestinationId { get; set; }
    public DocumentStorageProvider Provider { get; set; } = DocumentStorageProvider.NONE;
    public Guid? StorageConnectionId { get; set; }
    public string? SiteIdentifier { get; set; }
    public string? DriveIdentifier { get; set; }
    public string? FolderIdentifier { get; set; }
    public string? DestinationUrl { get; set; }
    public bool ExternalTransferEnabled { get; set; }
    public StorageAssignmentStatus Status { get; set; } = StorageAssignmentStatus.NOT_CONFIGURED;
    public DateTime? ValidatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; } = null!;
    public DocumentStorageConnection? StorageConnection { get; set; }
    public DocumentStorageDestination? DocumentStorageDestination { get; set; }
    public User CreatedByUser { get; set; } = null!;
}

public sealed class MicrosoftAuthorizationState
{
    public Guid Id { get; set; }
    public required string StateHash { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public required string ReturnUrl { get; set; }
    public required string DraftJson { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}