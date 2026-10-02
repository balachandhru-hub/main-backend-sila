using System.ComponentModel.DataAnnotations;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed record OrganizationResponse(
    Guid Id,
    Guid? ParentOrganizationId,
    string Code,
    string Name,
    OrganizationKind Kind,
    StatusKind Status,
    string? LogoUrl = null);

public sealed record OrganizationUnitResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? ParentUnitId,
    string Code,
    string Name,
    OrganizationUnitKind Kind,
    StatusKind Status);

public sealed record PermissionResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string Module = "Administration",
    string? SubModule = null,
    string ApplicationScope = "BOTH",
    string RiskLevel = "LOW",
    bool IsSystemAuthorization = true,
    bool IsActive = true);

public sealed record RoleResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    bool IsSystem,
    StatusKind Status,
    IReadOnlyList<PermissionResponse> Permissions,
    string ApplicationScope = "BOTH");

public sealed record AccessContextResponse(
    CurrentUserResponse User,
    IReadOnlyList<OrganizationResponse> Organizations,
    IReadOnlyList<OrganizationUnitResponse> Units,
    IReadOnlyList<RoleResponse> Roles,
    IReadOnlyList<PermissionResponse> Permissions);

public sealed record AccessUserResponse(
    Guid Id,
    string DisplayName,
    string Email,
    StatusKind Status,
    IReadOnlyList<string> Applications,
    IReadOnlyList<OrganizationResponse> Organizations,
    IReadOnlyList<OrganizationUnitResponse> Units,
    IReadOnlyList<RoleResponse> Roles,
    string? FirstName = null,
    string? LastName = null,
    string? MobileNumber = null,
    bool MustChangePassword = false);

public sealed record AuthorizationPreviewResponse(
    Guid PermissionId,
    string Key,
    string Name,
    string Module,
    string ApplicationScope,
    string RiskLevel,
    string Decision,
    string Source);

public sealed record StorageAssignmentResponse(
    DocumentStorageProvider Provider,
    StorageAssignmentStatus Status,
    Guid? StorageConnectionId,
    string? SiteIdentifier,
    string? DriveIdentifier,
    string? FolderIdentifier,
    string? DestinationUrl,
    bool ExternalTransferEnabled,
    string? ConnectionMessage,
    Guid? DestinationId = null,
    string? FolderPath = null);

public sealed record AccessUserDetailResponse(
    AccessUserResponse User,
    IReadOnlyList<AuthorizationPreviewResponse> EffectiveAuthorizations,
    IReadOnlyList<AuthorizationPreviewResponse> Overrides,
    StorageAssignmentResponse Storage);

public sealed record CreateAccessUserResponse(
    AccessUserResponse User,
    string TemporaryPassword,
    bool MustChangePassword);

public sealed record StorageConnectionResponse(
    Guid Id,
    Guid OrganizationId,
    DocumentStorageProvider Provider,
    string Name,
    StorageConnectionStatus ConnectionStatus,
    string? TenantIdentifier,
    string? SiteIdentifier,
    string? DriveIdentifier,
    string? FolderIdentifier,
    string? DisplayUrl,
    string? DisplayName,
    DateTime? ValidatedAt);

public sealed class CreateOrganizationRequest
{
    [Required, StringLength(64, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public OrganizationKind Kind { get; set; }

    public Guid? ParentOrganizationId { get; set; }
}

public sealed class CreateOrganizationUnitRequest
{
    [Required, StringLength(64, MinimumLength = 2)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public OrganizationUnitKind Kind { get; set; }

    public Guid? ParentUnitId { get; set; }
}

public sealed class CreateAccessUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 2)]
    public string DisplayName { get; set; } = string.Empty;

    [MinLength(8)]
    public string? Password { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MobileNumber { get; set; }
    public bool GenerateTemporaryPassword { get; set; } = true;

    [Required, MinLength(1)]
    public List<ApplicationKind> Applications { get; set; } = [];

    [Required]
    public Guid OrganizationId { get; set; }

    public Guid? OrganizationUnitId { get; set; }
    public Guid? RoleId { get; set; }
    public List<Guid> RoleIds { get; set; } = [];
    public List<Guid> OrganizationUnitIds { get; set; } = [];
    public List<Guid> GrantedAuthorizationIds { get; set; } = [];
    public List<Guid> DeniedAuthorizationIds { get; set; } = [];
    public DocumentStorageProvider StorageProvider { get; set; } = DocumentStorageProvider.NONE;
    public Guid? StorageConnectionId { get; set; }
    public string? StorageSiteIdentifier { get; set; }
    public string? StorageDriveIdentifier { get; set; }
    public string? StorageFolderIdentifier { get; set; }
    public string? StorageDestinationUrl { get; set; }
    public bool ExternalStorageTransferEnabled { get; set; }
}

public sealed class AssignMembershipRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    public Guid? OrganizationUnitId { get; set; }
}

public sealed class AssignRoleRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid RoleId { get; set; }

    [Required]
    public Guid OrganizationId { get; set; }

    public Guid? OrganizationUnitId { get; set; }
}

public sealed class AuthorizationOverrideRequest
{
    [Required]
    public Guid AuthorizationId { get; set; }

    [Required]
    public AuthorizationOverrideType OverrideType { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public sealed class CreateStorageConnectionRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public DocumentStorageProvider Provider { get; set; }

    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public string? TenantIdentifier { get; set; }
    public string? SiteIdentifier { get; set; }
    public string? DriveIdentifier { get; set; }
    public string? FolderIdentifier { get; set; }
    public string? DisplayUrl { get; set; }
}