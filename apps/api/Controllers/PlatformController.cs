using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController(
    PlatformDbContext platform,
    IPlatformAuthService auth,
    ITenantProvisioningService provisioning,
    ITenantLaunchService launches,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("healthz")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var connected = await platform.Database.CanConnectAsync(cancellationToken);
        return connected
            ? Ok(new { api = "OK", plane = "platform", database = "connected" })
            : StatusCode(503, new { api = "OK", plane = "platform", database = "unavailable" });
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new ApiError("INVALID_CREDENTIALS", "Invalid credentials."));
        }

        Response.Cookies.Append(PlatformSessionCookie.Name, result.Value.RawToken, CloudSessionCookie.Options(Request, configuration));
        return Ok(ToUser(result.Value.User));
    }

    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await auth.RevokeAsync(CurrentUser(), Request.Cookies[PlatformSessionCookie.Name], cancellationToken);
        Response.Cookies.Delete(PlatformSessionCookie.Name, new CookieOptions { Path = "/" });
        return NoContent();
    }

    [HttpGet("session")]
    public IActionResult Session()
    {
        var user = CurrentUser();
        return user is null ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid.")) : Ok(ToUser(user));
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants([FromQuery] string? query, [FromQuery] string? country, [FromQuery] string? status, [FromQuery] string? product, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.TenantsRead))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You do not have platform tenant access."));
        }

        var tenants = platform.Tenants.AsNoTracking()
            .Include(item => item.Environments).ThenInclude(item => item.Domains)
            .Include(item => item.ProductEntitlements)
            .Include(item => item.Licenses)
            .AsQueryable();
        if (!PlatformAuthorization.IsSuperAdmin(actor))
        {
            var assigned = actor.TenantAssignments
                .Where(item => item.Status == TenantAssignmentStatus.ACTIVE)
                .Select(item => item.TenantId)
                .ToHashSet();
            tenants = tenants.Where(item => assigned.Contains(item.Id));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            tenants = tenants.Where(item => item.CustomerName.ToLower().Contains(term) || item.TenantCode.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            tenants = tenants.Where(item => item.CountryCode == country);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TenantStatus>(status, true, out var parsed))
        {
            tenants = tenants.Where(item => item.Status == parsed);
        }
        else
        {
            tenants = tenants.Where(item => item.Status != TenantStatus.ARCHIVED);
        }

        if (!string.IsNullOrWhiteSpace(product))
        {
            tenants = tenants.Where(item => item.ProductEntitlements.Any(entitlement => entitlement.ProductCode == product && entitlement.Status == EntitlementStatus.ACTIVE));
        }

        var rows = await tenants.OrderBy(item => item.CustomerName).ToListAsync(cancellationToken);
        return Ok(rows.Select(ToTenantSummary));
    }

    [HttpGet("organizations")]
    public Task<IActionResult> Organizations([FromQuery] string? query, [FromQuery] string? country, [FromQuery] string? status, [FromQuery] string? product, CancellationToken cancellationToken) =>
        Tenants(query, country, status, product, cancellationToken);

    [HttpPost("organizations")]
    public Task<IActionResult> CreateOrganization([FromBody] OrganizationWriteRequest request, CancellationToken cancellationToken) =>
        CreateTenant(request.ToProvision(), cancellationToken);

    [HttpGet("tenants/{tenantId:guid}")]
    public async Task<IActionResult> Tenant(Guid tenantId, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.CanAccessTenant(actor, tenantId))
        {
            return StatusCode(403, new ApiError("TENANT_ACCESS_DENIED", "You are not assigned to this tenant."));
        }

        var tenant = await platform.Tenants.AsNoTracking()
            .Include(item => item.Environments).ThenInclude(item => item.Domains)
            .Include(item => item.Licenses)
            .Include(item => item.ProductEntitlements)
            .Include(item => item.ModuleEntitlements)
            .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        return tenant is null ? NotFound(new ApiError("TENANT_NOT_FOUND", "The tenant was not found.")) : Ok(ToTenantDetail(tenant));
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] ProvisionTenantRequest request, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.TenantsProvision))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot provision tenants."));
        }

        var tenant = await provisioning.ProvisionAsync(request, actor.Id, cancellationToken);
        await platform.Entry(tenant).Collection(item => item.Environments).LoadAsync(cancellationToken);
        return Ok(new
        {
            tenant.Id,
            tenant.TenantCode,
            organizationName = tenant.CustomerName,
            tenant.Status,
            environments = tenant.Environments.Select(item => new { item.Id, item.EnvironmentType, item.DatabaseSecretReference, item.RouteSlug, item.BaseUrl }),
        });
    }

    [HttpPatch("tenants/{tenantId:guid}")]
    [HttpPut("organizations/{tenantId:guid}")]
    public async Task<IActionResult> UpdateTenant(Guid tenantId, [FromBody] OrganizationWriteRequest request, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.TenantsManage) && !PlatformAuthorization.IsSuperAdmin(actor))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot edit organizations."));
        }

        var tenant = await platform.Tenants.Include(item => item.Environments)
            .Include(item => item.ProductEntitlements)
            .Include(item => item.Licenses)
            .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null) return NotFound(new ApiError("TENANT_NOT_FOUND", "The tenant was not found."));

        tenant.CustomerName = string.IsNullOrWhiteSpace(request.OrganizationName) ? tenant.CustomerName : request.OrganizationName.Trim();
        tenant.Address = request.Address?.Trim() ?? tenant.Address;
        tenant.CountryCode = request.Country?.Trim() ?? tenant.CountryCode;
        tenant.TaxRegistrationNumber = request.TaxRegistrationNumber?.Trim() ?? tenant.TaxRegistrationNumber;
        if (request.LicenseCount is int licenses) tenant.LicenseCount = licenses;
        if (request.TotalUsers is int users) tenant.TotalUsers = users;
        tenant.PrimaryContactName = request.PrimaryContactName?.Trim() ?? tenant.PrimaryContactName;
        tenant.PrimaryContactEmail = request.PrimaryContactEmail?.Trim() ?? tenant.PrimaryContactEmail;
        tenant.PrimaryContactPhone = request.PrimaryContactPhone?.Trim() ?? tenant.PrimaryContactPhone;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedByPlatformUserId = actor.Id;
        UpdateEnvironmentUrl(tenant, TenantEnvironmentType.PRODUCTION, request.ProductionUrl, request.ProductionRouteSlug);
        UpdateEnvironmentUrl(tenant, TenantEnvironmentType.TEST, request.TestUrl, request.TestRouteSlug);
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent { Id = Guid.NewGuid(), ActorPlatformUserId = actor.Id, Action = "TENANT_UPDATED", TenantId = tenantId, CreatedAt = DateTime.UtcNow });
        await platform.SaveChangesAsync(cancellationToken);
        return Ok(ToTenantSummary(tenant));
    }

    [HttpPost("tenants/{tenantId:guid}/archive")]
    [HttpDelete("organizations/{tenantId:guid}")]
    public async Task<IActionResult> Archive(Guid tenantId, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.IsSuperAdmin(actor) && !PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.TenantsManage))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot archive organizations."));
        }

        var tenant = await platform.Tenants.SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null) return NotFound(new ApiError("TENANT_NOT_FOUND", "The tenant was not found."));
        tenant.Status = TenantStatus.ARCHIVED;
        tenant.UpdatedAt = DateTime.UtcNow;
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent { Id = Guid.NewGuid(), ActorPlatformUserId = actor.Id, Action = "TENANT_ARCHIVED", TenantId = tenantId, CreatedAt = DateTime.UtcNow });
        await platform.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("tenants/{tenantId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid tenantId, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.TenantsSuspend))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot suspend tenants."));
        }

        var tenant = await platform.Tenants.SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null) return NotFound(new ApiError("TENANT_NOT_FOUND", "The tenant was not found."));
        tenant.Status = TenantStatus.SUSPENDED;
        tenant.UpdatedAt = DateTime.UtcNow;
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent { Id = Guid.NewGuid(), ActorPlatformUserId = actor.Id, Action = "TENANT_SUSPENDED", TenantId = tenantId, CreatedAt = DateTime.UtcNow });
        await platform.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("tenants/{tenantId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid tenantId, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.TenantsSuspend))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot reactivate tenants."));
        }

        var tenant = await platform.Tenants.SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);
        if (tenant is null) return NotFound(new ApiError("TENANT_NOT_FOUND", "The tenant was not found."));
        tenant.Status = TenantStatus.ACTIVE;
        tenant.UpdatedAt = DateTime.UtcNow;
        await platform.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("tenants/{tenantId:guid}/environments/{environmentId:guid}/launch")]
    public async Task<IActionResult> Launch(Guid tenantId, Guid environmentId, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var origin = Request.Headers.Origin.FirstOrDefault() ?? $"{Request.Scheme}://{Request.Host}";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var redirectUrl = await launches.CreateAsync(
            actor,
            tenantId,
            environmentId,
            origin,
            Request.Cookies[PlatformSessionCookie.Name],
            ip,
            cancellationToken);
        return Ok(new { redirectUrl });
    }

    [HttpPost("tenants/{tenantId:guid}/support-assignments")]
    public async Task<IActionResult> AssignSupport(Guid tenantId, [FromBody] SupportAssignmentRequest request, CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.SupportAssign))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot assign support access."));
        }

        platform.PlatformUserTenantAssignments.Add(new PlatformUserTenantAssignment
        {
            Id = Guid.NewGuid(),
            PlatformUserId = request.PlatformUserId,
            TenantId = tenantId,
            TenantEnvironmentId = request.EnvironmentId,
            PermissionsCsv = string.Join(',', request.Permissions ?? []),
            ValidFrom = request.ValidFrom ?? DateTime.UtcNow,
            ValidUntil = request.ValidUntil,
            Status = TenantAssignmentStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
        });
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent { Id = Guid.NewGuid(), ActorPlatformUserId = actor.Id, Action = "TENANT_ASSIGNMENT_ADDED", TenantId = tenantId, CreatedAt = DateTime.UtcNow });
        await platform.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreatePlatformUserRequest request,
        [FromServices] IPasswordService passwords,
        CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.IsSuperAdmin(actor))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "Only a Platform Super Admin can create platform users."));
        }

        var email = request.Email.Trim();
        var normalized = email.ToUpperInvariant();
        if (await platform.PlatformUsers.AnyAsync(item => item.NormalizedEmail == normalized, cancellationToken))
        {
            return Conflict(new ApiError("PLATFORM_USER_EXISTS", "A platform user with that email already exists."));
        }

        var now = DateTime.UtcNow;
        var user = new PlatformUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalized,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var probe = new User { Id = user.Id, Email = user.Email, NormalizedEmail = user.NormalizedEmail, DisplayName = user.DisplayName, PasswordHash = string.Empty };
        user.PasswordHash = passwords.HashPassword(probe, request.Password);
        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();
        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
        {
            user.DisplayName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(item => !string.IsNullOrWhiteSpace(item)));
        }
        platform.PlatformUsers.Add(user);
        var roleKey = string.IsNullOrWhiteSpace(request.RoleKey) ? "CUSTOMER_SUPPORT_CONSULTANT" : request.RoleKey.Trim();
        var role = await platform.PlatformRoles.SingleOrDefaultAsync(item => item.Key == roleKey, cancellationToken);
        if (role is null)
        {
            return BadRequest(new ApiError("PLATFORM_ROLE_NOT_FOUND", "The platform role was not found."));
        }

        platform.PlatformUserRoles.Add(new PlatformUserRole { Id = Guid.NewGuid(), PlatformUserId = user.Id, PlatformRoleId = role.Id, CreatedAt = now });
        if (request.AssignedOrganizationId is Guid assigned && assigned != Guid.Empty)
        {
            Guid? environmentId = null;
            if (!string.IsNullOrWhiteSpace(request.EnvironmentAccess) &&
                !string.Equals(request.EnvironmentAccess, "BOTH", StringComparison.OrdinalIgnoreCase))
            {
                var type = request.EnvironmentAccess.Equals("PRODUCTION", StringComparison.OrdinalIgnoreCase)
                    ? TenantEnvironmentType.PRODUCTION
                    : TenantEnvironmentType.TEST;
                environmentId = await platform.TenantEnvironments
                    .Where(item => item.TenantId == assigned && item.EnvironmentType == type)
                    .Select(item => (Guid?)item.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            platform.PlatformUserTenantAssignments.Add(new PlatformUserTenantAssignment
            {
                Id = Guid.NewGuid(),
                PlatformUserId = user.Id,
                TenantId = assigned,
                TenantEnvironmentId = environmentId,
                ValidFrom = now,
                Status = TenantAssignmentStatus.ACTIVE,
                CreatedAt = now,
            });
        }
        platform.PlatformAuditEvents.Add(new PlatformAuditEvent { Id = Guid.NewGuid(), ActorPlatformUserId = actor.Id, Action = "PLATFORM_USER_CREATED", CreatedAt = now, MetadataJson = user.Email });
        await platform.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.Email, user.DisplayName, role = role.Key });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.PlatformUsersManage) && !PlatformAuthorization.IsSuperAdmin(actor))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot manage platform users."));
        }

        var users = await platform.PlatformUsers.AsNoTracking()
            .Include(item => item.Roles).ThenInclude(item => item.Role)
            .OrderBy(item => item.DisplayName)
            .Select(item => new
            {
                item.Id,
                item.Email,
                item.DisplayName,
                item.FirstName,
                item.LastName,
                item.Status,
                Roles = item.Roles.Select(role => role.Role.Key),
            })
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit(CancellationToken cancellationToken)
    {
        var actor = RequireUser();
        if (actor is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!PlatformAuthorization.HasPermission(actor, PlatformPermissionKeys.AuditRead))
        {
            return StatusCode(403, new ApiError("PLATFORM_ACCESS_DENIED", "You cannot view platform audit."));
        }

        var events = await platform.PlatformAuditEvents.AsNoTracking().OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return Ok(events);
    }

    private static void UpdateEnvironmentUrl(Tenant tenant, TenantEnvironmentType type, string? url, string? slug)
    {
        if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(slug)) return;
        var environment = tenant.Environments.FirstOrDefault(item => item.EnvironmentType == type);
        if (environment is null) return;
        if (!string.IsNullOrWhiteSpace(url)) environment.BaseUrl = url.Trim();
        var resolved = slug ?? TenantRouteResolver.SlugFromUrl(url);
        if (!string.IsNullOrWhiteSpace(resolved)) environment.RouteSlug = TenantRouteResolver.NormalizeSlug(resolved);
        environment.UpdatedAt = DateTime.UtcNow;
    }

    private PlatformUser? CurrentUser() => HttpContext.Items[PlatformContext.ItemKey] as PlatformUser;
    private PlatformUser? RequireUser() => CurrentUser();

    private static object ToUser(PlatformUser user) => new
    {
        user.Id,
        user.DisplayName,
        user.Email,
        plane = "platform",
        roles = user.Roles.Select(item => item.Role.Key).ToArray(),
        permissions = user.Roles.SelectMany(item => item.Role.Permissions).Select(item => item.Permission.Key).Distinct().ToArray(),
        superAdmin = PlatformAuthorization.IsSuperAdmin(user),
    };

    private static object ToTenantSummary(Tenant tenant)
    {
        var test = tenant.Environments.FirstOrDefault(item => item.EnvironmentType is TenantEnvironmentType.TEST or TenantEnvironmentType.DEVELOPMENT);
        var prod = tenant.Environments.FirstOrDefault(item => item.EnvironmentType == TenantEnvironmentType.PRODUCTION);
        return new
        {
            tenant.Id,
            tenant.TenantCode,
            tenant.CustomerName,
            organizationName = tenant.CustomerName,
            tenant.Address,
            tenant.CountryCode,
            tenant.Status,
            tenant.LicenseCount,
            tenant.TotalUsers,
            products = tenant.ProductEntitlements.Where(item => item.Status == EntitlementStatus.ACTIVE).Select(item => item.ProductCode),
            testUrl = test?.BaseUrl ?? test?.Domains.FirstOrDefault()?.Hostname,
            productionUrl = prod?.BaseUrl ?? prod?.Domains.FirstOrDefault()?.Hostname,
            testEnvironmentId = test?.Id,
            productionEnvironmentId = prod?.Id,
            testRouteSlug = test?.RouteSlug,
            productionRouteSlug = prod?.RouteSlug,
            licenses = tenant.Licenses.Select(item => new { item.LicenseType, item.LicensedQuantity, item.Status }),
        };
    }

    private static object ToTenantDetail(Tenant tenant) => new
    {
        tenant.Id,
        tenant.TenantCode,
        tenant.CustomerName,
        organizationName = tenant.CustomerName,
        tenant.LegalName,
        tenant.Address,
        tenant.CountryCode,
        tenant.TaxRegistrationNumber,
        tenant.PrimaryContactName,
        tenant.PrimaryContactEmail,
        tenant.PrimaryContactPhone,
        tenant.Status,
        tenant.LicenseCount,
        tenant.TotalUsers,
        tenant.MaxCloudUsers,
        tenant.MaxMobileUsers,
        environments = tenant.Environments.Select(item => new
        {
            item.Id,
            item.EnvironmentType,
            item.DisplayName,
            item.Status,
            item.BaseUrl,
            item.RouteSlug,
            item.DataRegion,
            item.ProvisioningCheckpoint,
            domains = item.Domains.Select(domain => domain.Hostname),
        }),
        licenses = tenant.Licenses,
        products = tenant.ProductEntitlements,
        modules = tenant.ModuleEntitlements,
    };
}

public sealed class CreatePlatformUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Password { get; set; } = string.Empty;
    public string RoleKey { get; set; } = "CUSTOMER_SUPPORT_CONSULTANT";
    public Guid? AssignedOrganizationId { get; set; }
    public string? EnvironmentAccess { get; set; }
}

public sealed class OrganizationWriteRequest
{
    public string? OrganizationName { get; set; }
    public string? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string? CustomerName { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public int? LicenseCount { get; set; }
    public int? TotalUsers { get; set; }
    public int? CloudUsers { get; set; }
    public int? MobileUsers { get; set; }
    public string? PrimaryContactName { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? PrimaryContactPhone { get; set; }
    public string? ProductionUrl { get; set; }
    public string? TestUrl { get; set; }
    public string? ProductionRouteSlug { get; set; }
    public string? TestRouteSlug { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminDisplayName { get; set; }
    public string? AdminPassword { get; set; }
    public string? Status { get; set; }

    public ProvisionTenantRequest ToProvision()
    {
        var name = (OrganizationName ?? CustomerName ?? "").Trim();
        var code = (TenantId ?? TenantCode ?? "").Trim();
        var productionSlug = ProductionRouteSlug ?? TenantRouteResolver.SlugFromUrl(ProductionUrl);
        var testSlug = TestRouteSlug ?? TenantRouteResolver.SlugFromUrl(TestUrl);
        return new ProvisionTenantRequest(
            code,
            name,
            null,
            Country ?? CountryCode,
            TaxRegistrationNumber,
            Address,
            null,
            null,
            PrimaryContactName,
            PrimaryContactEmail,
            PrimaryContactPhone,
            null,
            null,
            null,
            CloudUsers ?? LicenseCount ?? 1,
            MobileUsers ?? 100,
            null,
            null,
            ["SILA_ME"],
            ProductCodes.DefaultModules,
            !string.IsNullOrWhiteSpace(TestUrl) || !string.IsNullOrWhiteSpace(testSlug),
            !string.IsNullOrWhiteSpace(ProductionUrl) || !string.IsNullOrWhiteSpace(productionSlug),
            null,
            null,
            TestUrl,
            ProductionUrl,
            AdminEmail ?? "",
            AdminDisplayName ?? name,
            AdminPassword ?? "",
            null,
            null,
            ["CLOUD"],
            LicenseCount,
            TotalUsers,
            productionSlug,
            testSlug);
    }
}

public sealed class SupportAssignmentRequest
{
    public Guid PlatformUserId { get; set; }
    public Guid? EnvironmentId { get; set; }
    public IReadOnlyList<string>? Permissions { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
}

[ApiController]
[Route("api")]
public sealed class PublicTenantController(ITenantContextAccessor tenants, IEntitlementService entitlements, PlatformDbContext platform) : ControllerBase
{
    [HttpGet("public/tenant")]
    public async Task<IActionResult> Current(CancellationToken cancellationToken)
    {
        var context = tenants.Current;
        if (context is null)
        {
            return NotFound(new ApiError("TENANT_DOMAIN_NOT_FOUND", "This hostname is not registered to a SILA tenant."));
        }

        var tenant = await platform.Tenants.AsNoTracking().SingleAsync(item => item.Id == context.TenantId, cancellationToken);
        return Ok(new
        {
            context.TenantId,
            context.TenantCode,
            context.CustomerName,
            environment = context.EnvironmentType.ToString(),
            environmentCode = TenantOperationalDatabase.EnvironmentCode(context.EnvironmentType),
            environmentId = context.EnvironmentId,
            routeSlug = context.RouteSlug,
            databaseName = TenantOperationalDatabase.Name(context.TenantCode, context.EnvironmentType),
            testEnvironment = context.EnvironmentType is TenantEnvironmentType.TEST or TenantEnvironmentType.DEVELOPMENT,
            loginWelcomeText = tenant.LoginWelcomeText,
            logoFileName = tenant.LogoFileName,
        });
    }

    [HttpGet("v1/entitlements/{moduleCode}")]
    public async Task<IActionResult> Module(string moduleCode, CancellationToken cancellationToken)
    {
        await entitlements.EnsureModuleAsync(moduleCode, cancellationToken);
        return Ok(new { moduleCode, entitled = true });
    }

    [HttpGet("v1/licenses")]
    public async Task<IActionResult> Licenses([FromServices] ILicenseService licenses, CancellationToken cancellationToken) =>
        Ok(await licenses.GetUsageAsync(cancellationToken));
}
