using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/access")]
public sealed class AccessController(
    SilaMeDbContext db,
    IAccessService accessService,
    IPasswordService passwordService,
    OrganizationBrandingService brandingService,
    ILicenseService licenses) : ControllerBase
{
    [HttpGet("context")]
    public async Task<IActionResult> GetContext(CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        return session is null
            ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."))
            : Ok(await accessService.GetContextAsync(session, cancellationToken));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(PermissionKeys.UserRead, null, null, cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        return Ok(await accessService.GetUsersAsync(session, cancellationToken));
    }

    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.UserRead, null, null, cancellationToken);
        if (session is null) return CurrentSession() is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : PermissionDenied();
        var user = await accessService.GetUserDetailAsync(session, userId, cancellationToken);
        return user is null ? NotFound(new ApiError("USER_NOT_FOUND", "The user was not found in your accessible scope.")) : Ok(user);
    }

    [HttpPut("users/{userId:guid}/overrides")]
    public async Task<IActionResult> SetAuthorizationOverride(
        Guid userId,
        [FromBody] AuthorizationOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.UserManage, null, null, cancellationToken);
        if (session is null) return CurrentSession() is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : PermissionDenied();
        var target = await accessService.GetUserDetailAsync(session, userId, cancellationToken);
        if (target is null) return NotFound(new ApiError("USER_NOT_FOUND", "The user was not found in your accessible scope."));
        if (await IsProtectedUserAsync(userId, cancellationToken)) return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_USER", "The protected administrator cannot be changed through this action."));
        var permission = await db.Permissions.SingleOrDefaultAsync(item => item.Id == request.AuthorizationId && item.IsActive, cancellationToken);
        if (permission is null) return NotFound(new ApiError("AUTHORIZATION_NOT_FOUND", "The authorization was not found."));
        var existing = await db.UserAuthorizationOverrides.SingleOrDefaultAsync(item => item.UserId == userId && item.AuthorizationId == request.AuthorizationId, cancellationToken);
        var now = DateTime.UtcNow;
        if (existing is null)
        {
            db.UserAuthorizationOverrides.Add(new UserAuthorizationOverride
            {
                Id = Guid.NewGuid(), UserId = userId, AuthorizationId = permission.Id, OverrideType = request.OverrideType,
                Reason = request.Reason?.Trim(), CreatedAt = now, CreatedByUserId = session.CustomerUserId(), UpdatedAt = now,
            });
        }
        else
        {
            existing.OverrideType = request.OverrideType;
            existing.Reason = request.Reason?.Trim();
            existing.UpdatedAt = now;
        }
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), UserId = userId, ChangedByUserId = session.UserId, EventType = "USER_AUTHORIZATION_OVERRIDE_CHANGED",
            EntityType = "UserAuthorizationOverride", EntityId = userId, Reference = permission.Key,
            NewStateJson = $"{{\"permission\":\"{permission.Key}\",\"decision\":\"{request.OverrideType}\"}}",
            Reason = request.Reason?.Trim(), Result = "SUCCESS", CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(await accessService.GetUserDetailAsync(session, userId, cancellationToken));
    }

    [HttpDelete("users/{userId:guid}/overrides/{authorizationId:guid}")]
    public async Task<IActionResult> RemoveAuthorizationOverride(Guid userId, Guid authorizationId, CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.UserManage, null, null, cancellationToken);
        if (session is null) return CurrentSession() is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : PermissionDenied();
        if (await IsProtectedUserAsync(userId, cancellationToken)) return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_USER", "The protected administrator cannot be changed through this action."));
        var existing = await db.UserAuthorizationOverrides.SingleOrDefaultAsync(item => item.UserId == userId && item.AuthorizationId == authorizationId, cancellationToken);
        if (existing is null) return NoContent();
        db.UserAuthorizationOverrides.Remove(existing);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), UserId = userId, ChangedByUserId = session.UserId, EventType = "USER_AUTHORIZATION_OVERRIDE_REMOVED",
            EntityType = "UserAuthorizationOverride", EntityId = userId, Reference = authorizationId.ToString(), Result = "SUCCESS", CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("storage-connections")]
    public async Task<IActionResult> GetStorageConnections([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.ViewDocumentStorage, organizationId, null, cancellationToken);
        if (session is null) return CurrentSession() is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : PermissionDenied();
        var connections = await db.DocumentStorageConnections.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Provider).ThenBy(item => item.Name)
            .Select(item => new StorageConnectionResponse(item.Id, item.OrganizationId, item.Provider, item.Name, item.ConnectionStatus,
                item.TenantIdentifier, item.SiteIdentifier, item.DriveIdentifier, item.FolderIdentifier, item.DisplayUrl, item.DisplayName, item.ValidatedAt))
            .ToListAsync(cancellationToken);
        return Ok(connections);
    }

    [HttpPost("storage-connections")]
    public async Task<IActionResult> CreateStorageConnection([FromBody] CreateStorageConnectionRequest request, CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.ManageDocumentStorage, request.OrganizationId, null, cancellationToken);
        if (session is null) return CurrentSession() is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : PermissionDenied();
        if (request.Provider == DocumentStorageProvider.NONE) return BadRequest(new ApiError("STORAGE_PROVIDER_REQUIRED", "Select an external provider or leave the user assignment as None."));
        var name = request.Name.Trim();
        if (await db.DocumentStorageConnections.AnyAsync(item => item.OrganizationId == request.OrganizationId && item.Provider == request.Provider && item.Name == name, cancellationToken))
            return Conflict(new ApiError("STORAGE_CONNECTION_EXISTS", "A storage connection with this name already exists."));
        var now = DateTime.UtcNow;
        var connection = new DocumentStorageConnection
        {
            Id = Guid.NewGuid(), OrganizationId = request.OrganizationId, Provider = request.Provider, Name = name,
            ConnectionStatus = request.Provider == DocumentStorageProvider.MICROSOFT
                ? StorageConnectionStatus.AUTHENTICATION_REQUIRED
                : StorageConnectionStatus.PENDING,
            TenantIdentifier = request.TenantIdentifier?.Trim(),
            SiteIdentifier = request.SiteIdentifier?.Trim(), DriveIdentifier = request.DriveIdentifier?.Trim(),
            FolderIdentifier = request.FolderIdentifier?.Trim(), DisplayUrl = request.DisplayUrl?.Trim(),
            CreatedAt = now, CreatedByUserId = session.CustomerUserId(), UpdatedAt = now,
        };
        db.DocumentStorageConnections.Add(connection);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = request.OrganizationId, ChangedByUserId = session.UserId,
            EventType = "DOCUMENT_STORAGE_CONNECTION_CREATED", EntityType = "DocumentStorageConnection",
            EntityId = connection.Id, Reference = request.Provider.ToString(), Result = "PENDING_CONFIGURATION", CreatedAt = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/access/storage-connections/{connection.Id}",
            new StorageConnectionResponse(connection.Id, connection.OrganizationId, connection.Provider, connection.Name, connection.ConnectionStatus,
                connection.TenantIdentifier, connection.SiteIdentifier, connection.DriveIdentifier, connection.FolderIdentifier, connection.DisplayUrl, connection.DisplayName, connection.ValidatedAt));
    }

    [HttpGet("authorization-preview")]
    public async Task<IActionResult> GetAuthorizationPreview(
        [FromQuery] Guid? roleId,
        [FromQuery] string? application,
        CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.RoleRead, null, null, cancellationToken);
        if (session is null) return CurrentSession() is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : PermissionDenied();
        var app = string.IsNullOrWhiteSpace(application) ? "CLOUD" : application.Trim().ToUpperInvariant();
        var roles = db.Roles.AsNoTracking().Where(role => role.Status == StatusKind.ACTIVE && (roleId == null || role.Id == roleId));
        var permissions = await roles.SelectMany(role => role.Permissions.Select(mapping => new
        {
            Permission = mapping.Permission,
            Role = role,
        })).Where(item => item.Permission.IsActive &&
            (item.Role.ApplicationScope == "BOTH" || item.Role.ApplicationScope == app) &&
            (item.Permission.ApplicationScope == "BOTH" || item.Permission.ApplicationScope == app))
            .Select(item => new AuthorizationPreviewResponse(
                item.Permission.Id, item.Permission.Key, item.Permission.Name, item.Permission.Module,
                item.Permission.ApplicationScope, item.Permission.RiskLevel, "GRANT", $"ROLE:{item.Role.Key}"))
            .Distinct()
            .OrderBy(item => item.Module).ThenBy(item => item.Key)
            .ToListAsync(cancellationToken);
        return Ok(permissions);
    }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(PermissionKeys.RoleRead, null, null, cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        var roles = await db.Roles
            .AsNoTracking()
            .Include(role => role.Permissions)
                .ThenInclude(mapping => mapping.Permission)
            .Where(role => role.Status == StatusKind.ACTIVE)
            .OrderBy(role => role.Name)
            .Select(role => new RoleResponse(
                role.Id,
                role.Key,
                role.Name,
                role.Description,
                role.IsSystem,
                role.Status,
                role.Permissions
                    .Select(mapping => new PermissionResponse(
                        mapping.Permission.Id,
                        mapping.Permission.Key,
                        mapping.Permission.Name,
                        mapping.Permission.Description,
                        mapping.Permission.Module,
                        mapping.Permission.SubModule,
                        mapping.Permission.ApplicationScope,
                        mapping.Permission.RiskLevel,
                        mapping.Permission.IsSystemAuthorization,
                        mapping.Permission.IsActive))
                    .OrderBy(permission => permission.Key)
                    .ToList(),
                role.ApplicationScope))
            .ToListAsync(cancellationToken);
        return Ok(roles);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(PermissionKeys.RoleRead, null, null, cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        return Ok(await db.Permissions.AsNoTracking().OrderBy(permission => permission.Key).Select(permission =>
            new PermissionResponse(permission.Id, permission.Key, permission.Name, permission.Description, permission.Module, permission.SubModule, permission.ApplicationScope, permission.RiskLevel, permission.IsSystemAuthorization, permission.IsActive))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(
            PermissionKeys.OrganizationManage,
            request.ParentOrganizationId,
            null,
            cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Organizations.AnyAsync(organization => organization.Code == code, cancellationToken))
        {
            return Conflict(new ApiError("ORGANIZATION_CODE_EXISTS", "An organization with this code already exists."));
        }

        if (request.ParentOrganizationId is not null &&
            !await db.Organizations.AnyAsync(organization =>
                organization.Id == request.ParentOrganizationId && organization.Status == StatusKind.ACTIVE, cancellationToken))
        {
            return NotFound(new ApiError("PARENT_ORGANIZATION_NOT_FOUND", "The parent organization was not found."));
        }

        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            ParentOrganizationId = request.ParentOrganizationId,
            Code = code,
            Name = request.Name.Trim(),
            Kind = request.Kind,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Organizations.Add(organization);
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = session.CustomerUserId(),
            OrganizationId = organization.Id,
            CreatedAt = now,
        });

        var adminRole = await db.Roles.SingleOrDefaultAsync(role => role.Key == "SUPER_ADMIN", cancellationToken);
        if (adminRole is not null)
        {
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = session.CustomerUserId(),
                RoleId = adminRole.Id,
                OrganizationId = organization.Id,
                CreatedAt = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/access/organizations/{organization.Id}", ToOrganization(organization));
    }

    [HttpGet("organizations/{organizationId:guid}/logo")]
    public async Task<IActionResult> GetOrganizationLogo(Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var organization = await db.Organizations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken);
        if (organization is null || string.IsNullOrWhiteSpace(organization.CustomerLogoFileName))
        {
            return NotFound(new ApiError("CUSTOMER_LOGO_NOT_FOUND", "No customer logo is configured for this organization."));
        }

        var canView = await IsSystemAdminAsync(session, cancellationToken) ||
            await accessService.HasPermissionAsync(session, PermissionKeys.OrganizationRead, organizationId, null, cancellationToken) ||
            await db.UserOrganizationMemberships.AsNoTracking().AnyAsync(item =>
                item.UserId == session.UserId && item.OrganizationId == organizationId, cancellationToken);
        if (!canView)
        {
            return PermissionDenied();
        }

        try
        {
            var stream = await brandingService.OpenAsync(organization.CustomerLogoFileName, cancellationToken);
            return File(stream, organization.CustomerLogoContentType ?? "application/octet-stream");
        }
        catch (FileNotFoundException)
        {
            return NotFound(new ApiError("CUSTOMER_LOGO_NOT_FOUND", "No customer logo is configured for this organization."));
        }
    }

    [HttpPut("organizations/{organizationId:guid}/logo")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadOrganizationLogo(
        Guid organizationId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        if (!await IsSystemAdminAsync(session, cancellationToken))
        {
            return PermissionDenied();
        }

        if (file is null)
        {
            return BadRequest(new ApiError("LOGO_REQUIRED", "Choose an image file to upload."));
        }

        var organization = await db.Organizations
            .SingleOrDefaultAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken);
        if (organization is null)
        {
            return NotFound(new ApiError("ORGANIZATION_NOT_FOUND", "The organization was not found."));
        }

        try
        {
            var previous = organization.CustomerLogoFileName;
            var stored = await brandingService.StoreAsync(organizationId, file, cancellationToken);
            organization.CustomerLogoFileName = stored.FileName;
            organization.CustomerLogoContentType = stored.ContentType;
            organization.CustomerLogoUpdatedAt = DateTime.UtcNow;
            organization.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            if (!string.Equals(previous, stored.FileName, StringComparison.OrdinalIgnoreCase))
            {
                brandingService.Delete(previous);
            }

            return Ok(ToOrganization(organization));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ApiError("INVALID_LOGO", exception.Message));
        }
    }

    [HttpDelete("organizations/{organizationId:guid}/logo")]
    public async Task<IActionResult> DeleteOrganizationLogo(Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        if (!await IsSystemAdminAsync(session, cancellationToken))
        {
            return PermissionDenied();
        }

        var organization = await db.Organizations
            .SingleOrDefaultAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken);
        if (organization is null)
        {
            return NotFound(new ApiError("ORGANIZATION_NOT_FOUND", "The organization was not found."));
        }

        brandingService.Delete(organization.CustomerLogoFileName);
        organization.CustomerLogoFileName = null;
        organization.CustomerLogoContentType = null;
        organization.CustomerLogoUpdatedAt = null;
        organization.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToOrganization(organization));
    }

    [HttpPost("organizations/{organizationId:guid}/units")]
    public async Task<IActionResult> CreateOrganizationUnit(
        Guid organizationId,
        [FromBody] CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(
            PermissionKeys.OrganizationManage,
            organizationId,
            request.ParentUnitId,
            cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        var organizationExists = await db.Organizations.AnyAsync(organization =>
            organization.Id == organizationId && organization.Status == StatusKind.ACTIVE, cancellationToken);
        if (!organizationExists)
        {
            return NotFound(new ApiError("ORGANIZATION_NOT_FOUND", "The organization was not found."));
        }

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.OrganizationUnits.AnyAsync(unit =>
                unit.OrganizationId == organizationId && unit.Code == code, cancellationToken))
        {
            return Conflict(new ApiError("ORGANIZATION_UNIT_CODE_EXISTS", "An operating unit with this code already exists."));
        }

        if (request.ParentUnitId is not null &&
            !await db.OrganizationUnits.AnyAsync(unit =>
                unit.Id == request.ParentUnitId && unit.OrganizationId == organizationId && unit.Status == StatusKind.ACTIVE,
                cancellationToken))
        {
            return NotFound(new ApiError("PARENT_UNIT_NOT_FOUND", "The parent operating unit was not found."));
        }

        var now = DateTime.UtcNow;
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ParentUnitId = request.ParentUnitId,
            Code = code,
            Name = request.Name.Trim(),
            Kind = request.Kind,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.OrganizationUnits.Add(unit);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/v1/access/organization-units/{unit.Id}", ToUnit(unit));
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateAccessUserRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(
            PermissionKeys.UserManage,
            request.OrganizationId,
            request.OrganizationUnitId,
            cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return Conflict(new ApiError("USER_ALREADY_EXISTS", "A user with this email already exists."));
        }

        var requestedUnitIds = request.OrganizationUnitIds
            .Concat(request.OrganizationUnitId is null ? [] : [request.OrganizationUnitId.Value])
            .Distinct()
            .ToList();
        if (!await ValidOrganizationScopeAsync(request.OrganizationId, requestedUnitIds, cancellationToken))
        {
            return NotFound(new ApiError("ACCESS_SCOPE_NOT_FOUND", "The organization or operating unit was not found."));
        }

        var isSystemAdmin = await IsSystemAdminAsync(session, cancellationToken);
        var canManageTarget = isSystemAdmin || await accessService.HasPermissionAsync(
            session, PermissionKeys.UserManage, request.OrganizationId, requestedUnitIds.FirstOrDefault(), cancellationToken);
        if (!canManageTarget)
        {
            return PermissionDenied();
        }

        var roleIds = request.RoleIds
            .Concat(request.RoleId is null ? [] : [request.RoleId.Value])
            .Distinct()
            .ToList();
        if (roleIds.Count == 0)
        {
            var viewer = await db.Roles.SingleOrDefaultAsync(role => role.Key == "VIEWER" && role.Status == StatusKind.ACTIVE, cancellationToken);
            if (viewer is not null) roleIds.Add(viewer.Id);
        }
        var roles = await db.Roles.Where(role => roleIds.Contains(role.Id) && role.Status == StatusKind.ACTIVE)
            .Include(role => role.Permissions).ThenInclude(mapping => mapping.Permission)
            .ToListAsync(cancellationToken);
        if (roles.Count != roleIds.Count)
        {
            return BadRequest(new ApiError("ROLE_NOT_FOUND", "One or more requested roles were not found."));
        }

        var applications = request.Applications.Distinct().ToList();
        if (applications.Count == 0)
        {
            return BadRequest(new ApiError("APPLICATION_REQUIRED", "Select at least one application."));
        }

        try
        {
            foreach (var application in applications)
            {
                await licenses.EnsureCanActivateUserAsync(application, cancellationToken);
            }
        }
        catch (TenantException exception)
        {
            return StatusCode(exception.StatusCode, new ApiError(exception.Code, exception.Message));
        }
        if (roles.Any(role => role.ApplicationScope != "BOTH" && !applications.Contains(Enum.Parse<ApplicationKind>(role.ApplicationScope, true))))
        {
            return BadRequest(new ApiError("ROLE_APPLICATION_MISMATCH", "Each selected role must be available in at least one selected application."));
        }
        if (!isSystemAdmin && roles.Any(role => role.Key is "SUPER_ADMIN" or "PLATFORM_ADMIN"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_ROLE_ASSIGNMENT_DENIED", "Customer administrators cannot assign platform roles."));
        }
        var grantedIds = request.GrantedAuthorizationIds.Distinct().ToList();
        var deniedIds = request.DeniedAuthorizationIds.Distinct().ToList();
        if (grantedIds.Intersect(deniedIds).Any())
        {
            return BadRequest(new ApiError("OVERRIDE_CONFLICT", "An authorization cannot be both granted and denied."));
        }
        var authorizationIds = grantedIds.Concat(deniedIds).Distinct().ToList();
        var authorizations = await db.Permissions
            .Where(permission => authorizationIds.Contains(permission.Id) && permission.IsActive)
            .ToListAsync(cancellationToken);
        if (authorizations.Count != authorizationIds.Count)
        {
            return BadRequest(new ApiError("AUTHORIZATION_NOT_FOUND", "One or more requested authorizations were not found."));
        }
        if (authorizations.Any(permission => permission.ApplicationScope != "BOTH" &&
                !applications.Contains(Enum.Parse<ApplicationKind>(permission.ApplicationScope, true))))
        {
            return BadRequest(new ApiError("AUTHORIZATION_APPLICATION_MISMATCH", "An authorization is not available in the selected application."));
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(request.Password)
            ? GenerateTemporaryPassword()
            : request.Password!;
        if (temporaryPassword.Length < 8)
        {
            return BadRequest(new ApiError("PASSWORD_TOO_SHORT", "Temporary passwords must contain at least 8 characters."));
        }
        var now = DateTime.UtcNow;
        var displayName = request.DisplayName.Trim();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName,
            PasswordHash = string.Empty,
            FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            MobileNumber = string.IsNullOrWhiteSpace(request.MobileNumber) ? null : request.MobileNumber.Trim(),
            MustChangePassword = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.PasswordHash = passwordService.HashPassword(user, temporaryPassword);
        db.Users.Add(user);

        foreach (var application in applications)
        {
            db.UserApplicationAccess.Add(new UserApplicationAccess
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Application = application,
                CreatedAt = now,
            });
        }

        var membershipScopes = requestedUnitIds.Count == 0
            ? new List<Guid?> { null }
            : requestedUnitIds.Select(id => (Guid?)id).ToList();
        foreach (var unitId in membershipScopes)
        {
            db.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = request.OrganizationId,
                OrganizationUnitId = unitId,
                CreatedAt = now,
            });
        }

        foreach (var role in roles)
        {
            foreach (var unitId in membershipScopes)
            {
                db.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleId = role.Id,
                    OrganizationId = request.OrganizationId,
                    OrganizationUnitId = unitId,
                    CreatedAt = now,
                });
            }
        }
        foreach (var permission in authorizations)
        {
            db.UserAuthorizationOverrides.Add(new UserAuthorizationOverride
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AuthorizationId = permission.Id,
                OverrideType = deniedIds.Contains(permission.Id) ? AuthorizationOverrideType.DENY : AuthorizationOverrideType.GRANT,
                Reason = "Initial user provisioning",
                CreatedAt = now,
                CreatedByUserId = session.CustomerUserId(),
            });
        }
        if (request.StorageProvider != DocumentStorageProvider.NONE)
        {
            var connection = request.StorageConnectionId is null ? null : await db.DocumentStorageConnections
                .SingleOrDefaultAsync(item => item.Id == request.StorageConnectionId && item.OrganizationId == request.OrganizationId, cancellationToken);
            if (request.StorageConnectionId is not null && (connection is null || connection.Provider != request.StorageProvider))
            {
                return BadRequest(new ApiError("STORAGE_CONNECTION_NOT_FOUND", "The selected storage connection is not configured for this organization and provider."));
            }
            if (request.StorageProvider == DocumentStorageProvider.MICROSOFT &&
                (connection is null || connection.ConnectionStatus != StorageConnectionStatus.CONNECTED))
            {
                return BadRequest(new ApiError("MICROSOFT_STORAGE_NOT_VALIDATED", "Microsoft SharePoint read/write access must be validated before creating this user."));
            }
            DocumentStorageDestination? destination = null;
            if (connection?.ConnectionStatus == StorageConnectionStatus.CONNECTED &&
                connection.Provider == DocumentStorageProvider.MICROSOFT &&
                !string.IsNullOrWhiteSpace(connection.FolderPath))
            {
                destination = await db.DocumentStorageDestinations.SingleOrDefaultAsync(item =>
                    item.OrganizationId == request.OrganizationId &&
                    item.StorageConnectionId == connection.Id &&
                    item.DocumentType == DocumentType.INVOICE &&
                    item.OperatingUnitId == request.OrganizationUnitId, cancellationToken);
                if (destination is null)
                {
                    destination = new DocumentStorageDestination
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = request.OrganizationId,
                        OperatingUnitId = request.OrganizationUnitId,
                        StorageConnectionId = connection.Id,
                        Provider = connection.Provider,
                        DocumentType = DocumentType.INVOICE,
                        FolderPath = connection.FolderPath,
                        CreatedByUserId = session.CustomerUserId(),
                        CreatedAt = now,
                    };
                    db.DocumentStorageDestinations.Add(destination);
                }
                destination.SiteIdentifier = connection.SiteIdentifier;
                destination.DriveIdentifier = connection.DriveIdentifier;
                destination.FolderIdentifier = connection.FolderIdentifier;
                destination.FolderPath = connection.FolderPath;
                destination.DisplayUrl = connection.DisplayUrl;
                destination.ExternalTransferEnabled = true;
                destination.Status = StorageDestinationStatus.ACTIVE;
                destination.ValidatedAt = connection.ValidatedAt;
                destination.UpdatedAt = now;
            }
            db.UserDocumentStorageAssignments.Add(new UserDocumentStorageAssignment
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DocumentStorageDestinationId = destination?.Id,
                Provider = request.StorageProvider,
                StorageConnectionId = connection?.Id,
                SiteIdentifier = connection?.SiteIdentifier ?? request.StorageSiteIdentifier,
                DriveIdentifier = connection?.DriveIdentifier ?? request.StorageDriveIdentifier,
                FolderIdentifier = connection?.FolderIdentifier ?? request.StorageFolderIdentifier,
                DestinationUrl = connection?.DisplayUrl ?? request.StorageDestinationUrl,
                ExternalTransferEnabled = request.ExternalStorageTransferEnabled,
                Status = connection?.ConnectionStatus == StorageConnectionStatus.CONNECTED
                    ? StorageAssignmentStatus.VALIDATED
                    : StorageAssignmentStatus.PENDING_VALIDATION,
                ValidatedAt = connection?.ConnectionStatus == StorageConnectionStatus.CONNECTED ? now : null,
                CreatedAt = now,
                CreatedByUserId = session.CustomerUserId(),
                UpdatedAt = now,
            });
        }
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            UserId = user.Id,
            ChangedByUserId = session.UserId,
            EventType = "USER_ACCESS_CREATED",
            EntityType = "User",
            EntityId = user.Id,
            Reference = user.Email,
            NewStateJson = $"{{\"applications\":{System.Text.Json.JsonSerializer.Serialize(applications)},\"roles\":{System.Text.Json.JsonSerializer.Serialize(roles.Select(role => role.Key))},\"scopeCount\":{membershipScopes.Count}}}",
            Result = "SUCCESS",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);
        var created = (await accessService.GetUsersAsync(session, cancellationToken)).Single(item => item.Id == user.Id);
        return Created($"/api/v1/access/users/{user.Id}", new CreateAccessUserResponse(created, temporaryPassword, true));
    }

    [HttpPost("memberships")]
    public async Task<IActionResult> AssignMembership(
        [FromBody] AssignMembershipRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(
            PermissionKeys.AccessManage,
            request.OrganizationId,
            request.OrganizationUnitId,
            cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        if (!await db.Users.AnyAsync(user => user.Id == request.UserId && user.Status == StatusKind.ACTIVE, cancellationToken) ||
            !await ValidOrganizationScopeAsync(request.OrganizationId, request.OrganizationUnitId, cancellationToken))
        {
            return NotFound(new ApiError("ACCESS_SCOPE_NOT_FOUND", "The user or organization scope was not found."));
        }
        if (await IsProtectedUserAsync(request.UserId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_USER", "The protected administrator cannot be changed through this action."));
        }

        var exists = await db.UserOrganizationMemberships.AnyAsync(membership =>
            membership.UserId == request.UserId &&
            membership.OrganizationId == request.OrganizationId &&
            membership.OrganizationUnitId == request.OrganizationUnitId, cancellationToken);
        if (!exists)
        {
            db.UserOrganizationMemberships.Add(new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                OrganizationUnitId = request.OrganizationUnitId,
                CreatedAt = DateTime.UtcNow,
            });
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(), OrganizationId = request.OrganizationId, OperatingUnitId = request.OrganizationUnitId,
                UserId = request.UserId, ChangedByUserId = session.UserId, EventType = "USER_SCOPE_ADDED",
                EntityType = "UserOrganizationMembership", EntityId = request.UserId, Result = "SUCCESS", CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("roles")]
    public async Task<IActionResult> AssignRole(
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentSession() is null)
        {
            return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        }

        var session = await RequireCloudPermissionAsync(
            PermissionKeys.AccessManage,
            request.OrganizationId,
            request.OrganizationUnitId,
            cancellationToken);
        if (session is null)
        {
            return PermissionDenied();
        }

        var requestedRole = await db.Roles.SingleOrDefaultAsync(role => role.Id == request.RoleId && role.Status == StatusKind.ACTIVE, cancellationToken);
        var isSystemAdmin = await IsSystemAdminAsync(session, cancellationToken);
        if (!await db.Users.AnyAsync(user => user.Id == request.UserId && user.Status == StatusKind.ACTIVE, cancellationToken) ||
            requestedRole is null ||
            !await ValidOrganizationScopeAsync(request.OrganizationId, request.OrganizationUnitId, cancellationToken))
        {
            return NotFound(new ApiError("ROLE_SCOPE_NOT_FOUND", "The user, role, or organization scope was not found."));
        }
        if (!isSystemAdmin && (requestedRole.Key is "SUPER_ADMIN" or "PLATFORM_ADMIN"))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_ROLE_ASSIGNMENT_DENIED", "Customer administrators cannot assign platform roles."));
        }
        if (await IsProtectedUserAsync(request.UserId, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_USER", "The protected administrator cannot be changed through this action."));
        }

        var exists = await db.UserRoleAssignments.AnyAsync(assignment =>
            assignment.UserId == request.UserId &&
            assignment.RoleId == request.RoleId &&
            assignment.OrganizationId == request.OrganizationId &&
            assignment.OrganizationUnitId == request.OrganizationUnitId, cancellationToken);
        if (!exists)
        {
            db.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                RoleId = request.RoleId,
                OrganizationId = request.OrganizationId,
                OrganizationUnitId = request.OrganizationUnitId,
                CreatedAt = DateTime.UtcNow,
            });
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(), OrganizationId = request.OrganizationId, OperatingUnitId = request.OrganizationUnitId,
                UserId = request.UserId, ChangedByUserId = session.UserId, EventType = "USER_ROLE_ADDED",
                EntityType = "UserRoleAssignment", EntityId = request.UserId, Reference = requestedRole.Key, Result = "SUCCESS", CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpDelete("memberships")]
    public async Task<IActionResult> RemoveMembership([FromBody] AssignMembershipRequest request, CancellationToken cancellationToken)
    {
        if (CurrentSession() is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var session = await RequireCloudPermissionAsync(PermissionKeys.AccessManage, request.OrganizationId, request.OrganizationUnitId, cancellationToken);
        if (session is null) return PermissionDenied();
        if (await IsProtectedUserAsync(request.UserId, cancellationToken)) return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_USER", "The protected administrator cannot be changed through this action."));
        var membership = await db.UserOrganizationMemberships.SingleOrDefaultAsync(item =>
            item.UserId == request.UserId && item.OrganizationId == request.OrganizationId && item.OrganizationUnitId == request.OrganizationUnitId, cancellationToken);
        if (membership is null) return NoContent();
        db.UserOrganizationMemberships.Remove(membership);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = request.OrganizationId, OperatingUnitId = request.OrganizationUnitId,
            UserId = request.UserId, ChangedByUserId = session.UserId, EventType = "USER_SCOPE_REMOVED",
            EntityType = "UserOrganizationMembership", EntityId = request.UserId, Result = "SUCCESS", CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("roles")]
    public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
    {
        if (CurrentSession() is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var session = await RequireCloudPermissionAsync(PermissionKeys.AccessManage, request.OrganizationId, request.OrganizationUnitId, cancellationToken);
        if (session is null) return PermissionDenied();
        if (await IsProtectedUserAsync(request.UserId, cancellationToken)) return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_USER", "The protected administrator cannot be changed through this action."));
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.RoleId, cancellationToken);
        if (role is null) return NotFound(new ApiError("ROLE_NOT_FOUND", "The role was not found."));
        if (!await IsSystemAdminAsync(session, cancellationToken) && (role.Key is "SUPER_ADMIN" or "PLATFORM_ADMIN"))
            return StatusCode(StatusCodes.Status403Forbidden, new ApiError("PROTECTED_ROLE_ASSIGNMENT_DENIED", "Customer administrators cannot remove platform roles."));
        var assignment = await db.UserRoleAssignments.SingleOrDefaultAsync(item =>
            item.UserId == request.UserId && item.RoleId == request.RoleId &&
            item.OrganizationId == request.OrganizationId && item.OrganizationUnitId == request.OrganizationUnitId, cancellationToken);
        if (assignment is null) return NoContent();
        db.UserRoleAssignments.Remove(assignment);
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), OrganizationId = request.OrganizationId, OperatingUnitId = request.OrganizationUnitId,
            UserId = request.UserId, ChangedByUserId = session.UserId, EventType = "USER_ROLE_REMOVED",
            EntityType = "UserRoleAssignment", EntityId = request.UserId, Reference = role.Key, Result = "SUCCESS", CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Session? CurrentSession() =>
        HttpContext.Items[SessionContext.ItemKey] as Session;

    private async Task<Session?> RequireCloudPermissionAsync(
        string permissionKey,
        Guid? organizationId,
        Guid? organizationUnitId,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null || session.Application != ApplicationKind.CLOUD)
        {
            return null;
        }

        return await accessService.HasPermissionAsync(
            session,
            permissionKey,
            organizationId,
            organizationUnitId,
            cancellationToken) ? session : null;
    }

    private Task<bool> ValidOrganizationScopeAsync(
        Guid organizationId,
        Guid? organizationUnitId,
        CancellationToken cancellationToken) =>
        db.Organizations.AnyAsync(organization =>
            organization.Id == organizationId && organization.Status == StatusKind.ACTIVE &&
            (organizationUnitId == null || organization.Units.Any(unit =>
                unit.Id == organizationUnitId && unit.Status == StatusKind.ACTIVE)),
            cancellationToken);

    private Task<bool> ValidOrganizationScopeAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> organizationUnitIds,
        CancellationToken cancellationToken) =>
        db.Organizations.AnyAsync(organization =>
            organization.Id == organizationId && organization.Status == StatusKind.ACTIVE &&
            organizationUnitIds.All(unitId => organization.Units.Any(unit =>
                unit.Id == unitId && unit.Status == StatusKind.ACTIVE)),
            cancellationToken);

    private Task<bool> IsSystemAdminAsync(Session session, CancellationToken cancellationToken) =>
        db.UserRoleAssignments.AsNoTracking().AnyAsync(assignment =>
            assignment.UserId == session.UserId &&
            assignment.Status == StatusKind.ACTIVE &&
            assignment.Role.Status == StatusKind.ACTIVE &&
            assignment.Role.Key == "SUPER_ADMIN", cancellationToken);

    private Task<bool> IsProtectedUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(user => user.Id == userId && user.NormalizedEmail == "BALA@CHERVIC.IN", cancellationToken);

    private static string GenerateTemporaryPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        var builder = new StringBuilder(16);
        foreach (var value in bytes)
        {
            builder.Append(alphabet[value % alphabet.Length]);
        }
        return builder.ToString();
    }

    private static OrganizationResponse ToOrganization(Organization organization) =>
        new(
            organization.Id,
            organization.ParentOrganizationId,
            organization.Code,
            organization.Name,
            organization.Kind,
            organization.Status,
            OrganizationBrandingService.LogoUrlFor(organization.Id, organization.CustomerLogoFileName));

    private static OrganizationUnitResponse ToUnit(OrganizationUnit unit) =>
        new(unit.Id, unit.OrganizationId, unit.ParentUnitId, unit.Code, unit.Name, unit.Kind, unit.Status);

    private IActionResult PermissionDenied() =>
        StatusCode(StatusCodes.Status403Forbidden, new ApiError("PERMISSION_DENIED", "You do not have permission to perform this action."));
}