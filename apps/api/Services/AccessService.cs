using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class PermissionKeys
{
    public const string OrganizationRead = "organization.read";
    public const string OrganizationManage = "organization.manage";
    public const string UserRead = "user.read";
    public const string UserManage = "user.manage";
    public const string RoleRead = "role.read";
    public const string RoleManage = "role.manage";
    public const string AccessManage = "access.manage";
    public const string ScanInvoice = "SCAN_INVOICE";
    public const string UploadInvoice = "UPLOAD_INVOICE";
    public const string ViewInvoice = "VIEW_INVOICE";
    public const string EditInvoice = "EDIT_INVOICE";
    public const string ViewPurchaseOrder = "VIEW_PURCHASE_ORDER";
    public const string MatchPurchaseOrder = "MATCH_PURCHASE_ORDER";
    public const string PostGrn = "POST_GRN";
    public const string ViewGrn = "VIEW_GRN";
    public const string CreateGrn = "CREATE_GRN";
    public const string EditGrn = "EDIT_GRN";
    public const string ValidateGrn = "VALIDATE_GRN";
    public const string ViewExtraction = "VIEW_EXTRACTION";
    public const string EditExtraction = "EDIT_EXTRACTION";
    public const string ViewAgentConfig = "VIEW_AGENT_CONFIG";
    public const string ManageAgentConfig = "MANAGE_AGENT_CONFIG";
    public const string TestAgentConfig = "TEST_AGENT_CONFIG";
    public const string ManageApplicationAccess = "MANAGE_APPLICATION_ACCESS";
    public const string AssignRole = "ASSIGN_ROLE";
    public const string AssignScope = "ASSIGN_SCOPE";
    public const string ManagePermission = "MANAGE_PERMISSION";
    public const string ViewPermission = "VIEW_PERMISSION";
    public const string ViewRole = "VIEW_ROLE";
    public const string CreateRole = "CREATE_ROLE";
    public const string EditRole = "EDIT_ROLE";
    public const string ManageDocumentStorage = "CONFIGURE_DOCUMENT_STORAGE";
    public const string ViewDocumentStorage = "VIEW_DOCUMENT_STORAGE";
    public const string ValidateDocumentStorage = "VALIDATE_DOCUMENT_STORAGE";
    public const string DisconnectDocumentStorage = "DISCONNECT_DOCUMENT_STORAGE";
    public const string ViewAuditLog = "VIEW_AUDIT_LOG";
    public const string ManageIntegrationMapping = "MANAGE_INTEGRATION_MAPPING";
    public const string RunIntegration = "RUN_INTEGRATION";
    public const string ViewIntegrationLogs = "VIEW_INTEGRATION_LOGS";
    public const string ViewIntegrationData = "VIEW_INTEGRATION_DATA";
    public const string ViewIntegration = "VIEW_INTEGRATION";
    public const string ManageIntegration = "MANAGE_INTEGRATION";
    public const string TestIntegration = "TEST_INTEGRATION";

    public sealed record PermissionDefinition(
        string Key,
        string Name,
        string Description,
        string Module,
        string ApplicationScope = "BOTH",
        string RiskLevel = "LOW",
        string? SubModule = null,
        bool IsSystemAuthorization = true);

    private static PermissionDefinition D(
        string key,
        string name,
        string module,
        string applicationScope = "BOTH",
        string riskLevel = "LOW",
        string? description = null,
        string? subModule = null) =>
        new(key, name, description ?? name, module, applicationScope, riskLevel, subModule);

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        D(OrganizationRead, "View organization", "Organization", description: "View organizations and operating units."),
        D(OrganizationManage, "Manage organization", "Organization", riskLevel: "HIGH", description: "Create and update organization structure."),
        D(UserRead, "View users", "Administration", description: "View users and their access context."),
        D(UserManage, "Manage users", "Administration", riskLevel: "HIGH", description: "Create users and update application access."),
        D(RoleRead, "View roles", "Administration"),
        D(RoleManage, "Manage roles", "Administration", riskLevel: "HIGH"),
        D(AccessManage, "Manage access", "Administration", riskLevel: "HIGH"),
        D(ScanInvoice, "Scan invoice", "Documents", "MOBILE"),
        D(UploadInvoice, "Upload invoice", "Documents"),
        D(ViewInvoice, "View invoices", "Invoices"),
        D(EditInvoice, "Edit invoices", "Invoices"),
        D(ViewPurchaseOrder, "View purchase orders", "Procurement"),
        D(MatchPurchaseOrder, "Match purchase orders", "Procurement", riskLevel: "MEDIUM"),
        D(PostGrn, "Post goods receipts", "GRN", riskLevel: "CRITICAL"),
        D(ViewGrn, "View goods receipts", "GRN"),
        D(ViewExtraction, "View extraction", "Documents"),
        D(EditExtraction, "Edit extraction", "Documents", riskLevel: "MEDIUM"),
        D(ViewAgentConfig, "View extraction agents", "Integration"),
        D(ManageAgentConfig, "Manage extraction agents", "Integration", riskLevel: "HIGH"),
        D(TestAgentConfig, "Test extraction agents", "Integration", riskLevel: "MEDIUM"),
        D("VIEW_PLATFORM", "View platform", "Administration", "CLOUD"),
        D("MANAGE_PLATFORM", "Manage platform", "Administration", "CLOUD", "CRITICAL"),
        D("VIEW_CUSTOMER", "View customers", "Administration", "CLOUD"),
        D("CREATE_CUSTOMER", "Create customer", "Administration", "CLOUD", "HIGH"),
        D("EDIT_CUSTOMER", "Edit customer", "Administration", "CLOUD", "HIGH"),
        D("ACTIVATE_CUSTOMER", "Activate customer", "Administration", "CLOUD", "HIGH"),
        D("DEACTIVATE_CUSTOMER", "Deactivate customer", "Administration", "CLOUD", "CRITICAL"),
        D("VIEW_SUPPORT_ACCESS", "View support access", "Administration", "CLOUD"),
        D("GRANT_SUPPORT_ACCESS", "Grant support access", "Administration", "CLOUD", "HIGH"),
        D("REVOKE_SUPPORT_ACCESS", "Revoke support access", "Administration", "CLOUD", "HIGH"),
        D("ACCESS_CUSTOMER_SUPPORT_MODE", "Access customer support mode", "Administration", "CLOUD", "HIGH"),
        D("VIEW_PROPERTY", "View property", "Organization"),
        D("CREATE_PROPERTY", "Create property", "Organization", riskLevel: "HIGH"),
        D("EDIT_PROPERTY", "Edit property", "Organization", riskLevel: "HIGH"),
        D("VIEW_OPERATING_UNIT", "View operating unit", "Organization"),
        D("CREATE_OPERATING_UNIT", "Create operating unit", "Organization", riskLevel: "HIGH"),
        D("EDIT_OPERATING_UNIT", "Edit operating unit", "Organization", riskLevel: "HIGH"),
        D("VIEW_LOCATION", "View location", "Organization"),
        D("MANAGE_LOCATION", "Manage location", "Organization", riskLevel: "HIGH"),
        D("VIEW_USER", "View user", "Administration"),
        D("CREATE_USER", "Create user", "Administration", riskLevel: "HIGH"),
        D("EDIT_USER", "Edit user", "Administration", riskLevel: "HIGH"),
        D("ACTIVATE_USER", "Activate user", "Administration", riskLevel: "HIGH"),
        D("DEACTIVATE_USER", "Deactivate user", "Administration", riskLevel: "CRITICAL"),
        D(ManageApplicationAccess, "Manage application access", "Administration", riskLevel: "CRITICAL"),
        D(ViewRole, "View role", "Administration"),
        D(CreateRole, "Create role", "Administration", riskLevel: "HIGH"),
        D(EditRole, "Edit role", "Administration", riskLevel: "HIGH"),
        D(AssignRole, "Assign role", "Administration", riskLevel: "CRITICAL"),
        D(AssignScope, "Assign scope", "Administration", riskLevel: "HIGH"),
        D(ViewPermission, "View permission", "Administration"),
        D(ManagePermission, "Manage permission", "Administration", riskLevel: "CRITICAL"),
        D("VIEW_DASHBOARD", "View dashboard", "Dashboard"),
        D("VIEW_OPERATION_ALERTS", "View operation alerts", "Dashboard"),
        D("VIEW_KPI", "View KPIs", "Dashboard"),
        D("VIEW_MENU", "View menus", "Menu Engineering", "CLOUD"),
        D("CREATE_MENU", "Create menu", "Menu Engineering", "CLOUD"),
        D("EDIT_MENU", "Edit menu", "Menu Engineering", "CLOUD"),
        D("FINALIZE_MENU", "Finalize menu", "Menu Engineering", "CLOUD", "HIGH"),
        D("VIEW_RECIPE", "View recipes", "Recipe Management", "CLOUD"),
        D("CREATE_RECIPE", "Create recipe", "Recipe Management", "CLOUD"),
        D("EDIT_RECIPE", "Edit recipe", "Recipe Management", "CLOUD"),
        D("APPROVE_RECIPE", "Approve recipe", "Recipe Management", "CLOUD", "HIGH"),
        D("MANAGE_RECIPE_WORKFLOW", "Configure recipe approvals", "Recipe Management", "CLOUD", "HIGH"),
        D("VIEW_POS_INTEGRATION", "View POS integration", "Recipe Management", "CLOUD"),
        D("MANAGE_POS_INTEGRATION", "Manage POS integration", "Recipe Management", "CLOUD", "HIGH"),
        D("VIEW_RECIPE_TRANSACTION", "View recipe transaction tracker", "Recipe Management", "CLOUD"),
        D("REPROCESS_RECIPE_TRANSACTION", "Reprocess recipe consumption", "Recipe Management", "CLOUD", "HIGH"),
        D("VIEW_MATERIAL", "View materials", "Master Data"),
        D("CREATE_MATERIAL", "Create material", "Master Data", riskLevel: "HIGH"),
        D("EDIT_MATERIAL", "Edit material", "Master Data", riskLevel: "HIGH"),
        D("APPROVE_MATERIAL", "Approve material", "Master Data", "CLOUD", "HIGH"),
        D("VIEW_INVENTORY", "View inventory", "Inventory"),
        D("VIEW_STOCK_BALANCE", "View stock balance", "Inventory"),
        D("VIEW_STOCK_TRANSACTION", "View stock transactions", "Inventory"),
        D("CREATE_INVENTORY_COUNT", "Create inventory count", "Inventory", "BOTH"),
        D("ENTER_INVENTORY_COUNT", "Enter inventory count", "Inventory", "MOBILE"),
        D("APPROVE_INVENTORY_COUNT", "Approve inventory count", "Inventory", riskLevel: "HIGH"),
        D("POST_INVENTORY_COUNT", "Post inventory count", "Inventory", riskLevel: "CRITICAL"),
        D("CREATE_STOCK_ADJUSTMENT", "Create stock adjustment", "Inventory", riskLevel: "HIGH"),
        D("APPROVE_STOCK_ADJUSTMENT", "Approve stock adjustment", "Inventory", riskLevel: "HIGH"),
        D("POST_STOCK_ADJUSTMENT", "Post stock adjustment", "Inventory", riskLevel: "CRITICAL"),
        D("POST_OPENING_STOCK", "Post opening stock", "Inventory", riskLevel: "CRITICAL"),
        D("CREATE_STOCK_TRANSFER", "Create stock transfer", "Inventory", "MOBILE"),
        D("CREATE_INTERNAL_TRANSFER", "Create internal transfer order", "Inventory"),
        D("APPROVE_STOCK_TRANSFER", "Approve stock transfer", "Inventory", riskLevel: "HIGH"),
        D("DISPATCH_STOCK_TRANSFER", "Dispatch stock transfer", "Inventory", "MOBILE"),
        D("DISPATCH_INTERNAL_TRANSFER", "Dispatch internal transfer order", "Inventory"),
        D("RECEIVE_STOCK_TRANSFER", "Receive stock transfer", "Inventory", "MOBILE"),
        D("RECEIVE_INTERNAL_TRANSFER", "Receive internal transfer order", "Inventory"),
        D("VIEW_GOODS_ISSUE", "View goods issue", "Inventory"),
        D("CREATE_GOODS_ISSUE", "Create goods issue", "Inventory", "MOBILE"),
        D("POST_GOODS_ISSUE", "Post goods issue", "Inventory", "MOBILE", "CRITICAL"),
        D("VIEW_WASTE", "View waste", "Inventory"),
        D("CREATE_WASTE", "Create waste", "Inventory", "MOBILE"),
        D("POST_WASTE", "Post waste", "Inventory", "MOBILE", "CRITICAL"),
        D("VIEW_PURCHASE_SUGGESTION", "View purchase suggestions", "Procurement", "CLOUD"),
        D("APPROVE_PURCHASE_SUGGESTION", "Approve purchase suggestions", "Procurement", "CLOUD", "HIGH"),
        D("VIEW_SUPPLIER", "View suppliers", "Master Data"),
        D("CREATE_SUPPLIER", "Create supplier", "Master Data", riskLevel: "HIGH"),
        D("EDIT_SUPPLIER", "Edit supplier", "Master Data", riskLevel: "HIGH"),
        D("VIEW_RECEIVING", "View receiving", "Receiving"),
        D("ENTER_RECEIVED_QUANTITY", "Enter received quantity", "Receiving", "MOBILE"),
        D("ENTER_ACCEPTED_QUANTITY", "Enter accepted quantity", "Receiving", "MOBILE"),
        D("ENTER_DAMAGED_QUANTITY", "Enter damaged quantity", "Receiving", "MOBILE"),
        D("ENTER_REJECTED_QUANTITY", "Enter rejected quantity", "Receiving", "MOBILE"),
        D("VIEW_RECEIVING_EXCEPTION", "View receiving exceptions", "Receiving"),
        D("RESOLVE_RECEIVING_EXCEPTION", "Resolve receiving exceptions", "Receiving", riskLevel: "HIGH"),
        D("VIEW_DOCUMENT", "View documents", "Documents"),
        D("DOWNLOAD_DOCUMENT", "Download documents", "Documents"),
        D("ARCHIVE_DOCUMENT", "Archive documents", "Documents", riskLevel: "HIGH"),
        D("RUN_BASIC_OCR", "Run basic OCR", "Documents"),
        D("REREAD_INVOICE", "Re-read invoice", "Documents"),
        D("VIEW_EXTRACTION_HISTORY", "View extraction history", "Documents"),
        D("MANAGE_OCR_PROVIDER", "Manage OCR provider", "Integration", "CLOUD", "CRITICAL"),
        D("REPROCESS_INVOICE", "Reprocess invoice", "Invoices", riskLevel: "HIGH"),
        D("MATCH_SUPPLIER", "Match supplier", "Invoices", riskLevel: "MEDIUM"),
        D("MATCH_INVOICE_LINE", "Match invoice line", "Invoices", riskLevel: "MEDIUM"),
        D("VIEW_INVOICE_EXCEPTION", "View invoice exceptions", "Invoices"),
        D("RESOLVE_INVOICE_EXCEPTION", "Resolve invoice exceptions", "Invoices", riskLevel: "HIGH"),
        D("CREATE_GRN", "Create GRN", "GRN", "BOTH", "HIGH"),
        D("EDIT_GRN", "Edit GRN", "GRN", "BOTH", "HIGH"),
        D("VALIDATE_GRN", "Validate GRN", "GRN", "BOTH", "HIGH"),
        D("CANCEL_GRN", "Cancel GRN", "GRN", "BOTH", "CRITICAL"),
        D("VIEW_GRN_HISTORY", "View GRN history", "GRN"),
        D("VIEW_APPROVAL", "View approvals", "Approvals"),
        D("APPROVE", "Approve", "Approvals", "BOTH", "CRITICAL"),
        D("REJECT", "Reject", "Approvals", "BOTH", "HIGH"),
        D("RETURN_FOR_CORRECTION", "Return for correction", "Approvals", "BOTH", "HIGH"),
        D("VIEW_APPROVAL_HISTORY", "View approval history", "Approvals"),
        D("VIEW_ANALYTICS", "View analytics", "Analytics", "CLOUD"),
        D("VIEW_REPORT", "View reports", "Analytics", "CLOUD"),
        D("EXPORT_REPORT", "Export reports", "Analytics", "CLOUD", "MEDIUM"),
        D("VIEW_INTEGRATION", "View integrations", "Integration", "CLOUD"),
        D("MANAGE_INTEGRATION", "Manage integrations", "Integration", "CLOUD", "CRITICAL"),
        D("TEST_INTEGRATION", "Test integrations", "Integration", "CLOUD", "HIGH"),
        D(ManageIntegrationMapping, "Manage integration mappings", "Integration", "CLOUD", "HIGH"),
        D(RunIntegration, "Run integrations", "Integration", "CLOUD", "HIGH"),
        D(ViewIntegrationLogs, "View integration logs", "Integration", "CLOUD"),
        D(ViewIntegrationData, "View integration data updates", "Integration", "CLOUD"),
        D("VIEW_CONFIGURATION", "View configuration", "Administration", "CLOUD"),
        D("EDIT_CONFIGURATION", "Edit configuration", "Administration", "CLOUD", "HIGH"),
        D(ViewAuditLog, "View audit log", "Administration", "CLOUD"),
        D("VIEW_SECURITY_AUDIT", "View security audit", "Administration", "CLOUD", "HIGH"),
        D("EXPORT_AUDIT_LOG", "Export audit log", "Administration", "CLOUD", "HIGH"),
        D("VIEW_SYSTEM_DIAGNOSTICS", "View system diagnostics", "Administration", "CLOUD"),
        D("VIEW_SYSTEM_HEALTH", "View system health", "Administration", "CLOUD"),
        D(ViewDocumentStorage, "View document storage", "Storage", "CLOUD"),
        D(ManageDocumentStorage, "Configure document storage", "Storage", "CLOUD", "CRITICAL"),
        D(ValidateDocumentStorage, "Validate document storage", "Storage", "CLOUD", "HIGH"),
        D(DisconnectDocumentStorage, "Disconnect document storage", "Storage", "CLOUD", "CRITICAL"),
        D("TEST_DOCUMENT_STORAGE", "Test document storage", "Storage", "CLOUD", "HIGH"),
        D("VIEW_DOCUMENT_STORAGE_STATUS", "View document storage status", "Storage", "CLOUD"),
    ];
}

public interface IAccessService
{
    Task<AccessContextResponse> GetContextAsync(Session session, CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(Session session, string permissionKey, Guid? organizationId, Guid? organizationUnitId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccessUserResponse>> GetUsersAsync(Session session, CancellationToken cancellationToken);
    Task<AccessUserDetailResponse?> GetUserDetailAsync(Session session, Guid userId, CancellationToken cancellationToken);
}

public sealed class AccessService(SilaMeDbContext db, IUserService userService) : IAccessService
{
    public async Task<AccessContextResponse> GetContextAsync(Session session, CancellationToken cancellationToken)
    {
        if (session.IsSupportSession)
        {
            return await GetSupportContextAsync(session, cancellationToken);
        }

        var memberships = await db.UserOrganizationMemberships
            .AsNoTracking()
            .Include(membership => membership.Organization)
            .Include(membership => membership.OrganizationUnit)
            .Where(membership => membership.UserId == session.UserId && membership.Status == StatusKind.ACTIVE)
            .ToListAsync(cancellationToken);

        var assignments = await db.UserRoleAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Role)
                .ThenInclude(role => role.Permissions)
                    .ThenInclude(mapping => mapping.Permission)
            .Include(assignment => assignment.Organization)
            .Include(assignment => assignment.OrganizationUnit)
            .Where(assignment => assignment.UserId == session.UserId && assignment.Status == StatusKind.ACTIVE)
            .ToListAsync(cancellationToken);

        var overrides = await db.UserAuthorizationOverrides
            .AsNoTracking()
            .Where(item => item.UserId == session.UserId &&
                (item.EffectiveFrom == null || item.EffectiveFrom <= DateTime.UtcNow) &&
                (item.EffectiveTo == null || item.EffectiveTo > DateTime.UtcNow))
            .ToListAsync(cancellationToken);
        var deniedIds = overrides.Where(item => item.OverrideType == AuthorizationOverrideType.DENY).Select(item => item.AuthorizationId).ToHashSet();
        var grantedIds = overrides.Where(item => item.OverrideType == AuthorizationOverrideType.GRANT).Select(item => item.AuthorizationId).ToHashSet();
        var permissions = assignments
            .SelectMany(assignment => assignment.Role.Permissions.Select(mapping => mapping.Permission))
            .Where(permission => permission.IsActive &&
                (permission.ApplicationScope == "BOTH" || permission.ApplicationScope == session.Application.ToString()))
            .Where(permission => !deniedIds.Contains(permission.Id))
            .DistinctBy(permission => permission.Id)
            .Concat(await db.Permissions.AsNoTracking()
                .Where(permission => permission.IsActive &&
                    grantedIds.Contains(permission.Id) &&
                    (permission.ApplicationScope == "BOTH" || permission.ApplicationScope == session.Application.ToString()))
                .ToListAsync(cancellationToken))
            .DistinctBy(permission => permission.Id)
            .OrderBy(permission => permission.Key)
            .Select(ToPermission)
            .ToList();

        var organizations = memberships
            .Select(membership => membership.Organization)
            .Concat(assignments.Select(assignment => assignment.Organization))
            .DistinctBy(organization => organization.Id)
            .OrderBy(organization => organization.Name)
            .Select(ToOrganization)
            .ToList();

        var units = memberships
            .Where(membership => membership.OrganizationUnit is not null)
            .Select(membership => membership.OrganizationUnit!)
            .Concat(assignments.Where(assignment => assignment.OrganizationUnit is not null).Select(assignment => assignment.OrganizationUnit!))
            .DistinctBy(unit => unit.Id)
            .OrderBy(unit => unit.Name)
            .Select(ToUnit)
            .ToList();

        var roles = assignments
            .Select(assignment => assignment.Role)
            .DistinctBy(role => role.Id)
            .OrderBy(role => role.Name)
            .Select(ToRole)
            .ToList();

        return new AccessContextResponse(
            userService.ToResponse(session.User, session.Application, session),
            organizations,
            units,
            roles,
            permissions);
    }

    public async Task<bool> HasPermissionAsync(
        Session session,
        string permissionKey,
        Guid? organizationId,
        Guid? organizationUnitId,
        CancellationToken cancellationToken)
    {
        if (session.IsSupportSession)
        {
            return DelegatedPermissionKeys(session).Contains(permissionKey);
        }

        var user = await db.Users
            .AsNoTracking()
            .Include(candidate => candidate.ApplicationAccess)
            .SingleOrDefaultAsync(candidate => candidate.Id == session.UserId, cancellationToken);
        if (user is null || user.Status != StatusKind.ACTIVE ||
            !user.ApplicationAccess.Any(access => access.Application == session.Application && access.Status == StatusKind.ACTIVE))
        {
            return false;
        }

        var permission = await db.Permissions.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Key == permissionKey && candidate.IsActive &&
                (candidate.ApplicationScope == "BOTH" || candidate.ApplicationScope == session.Application.ToString()),
            cancellationToken);
        if (permission is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var hasDeny = await db.UserAuthorizationOverrides.AsNoTracking().AnyAsync(item =>
            item.UserId == session.UserId && item.AuthorizationId == permission.Id &&
            item.OverrideType == AuthorizationOverrideType.DENY &&
            (item.EffectiveFrom == null || item.EffectiveFrom <= now) &&
            (item.EffectiveTo == null || item.EffectiveTo > now), cancellationToken);
        if (hasDeny)
        {
            return false;
        }

        var hasGrant = await db.UserAuthorizationOverrides.AsNoTracking().AnyAsync(item =>
            item.UserId == session.UserId && item.AuthorizationId == permission.Id &&
            item.OverrideType == AuthorizationOverrideType.GRANT &&
            (item.EffectiveFrom == null || item.EffectiveFrom <= now) &&
            (item.EffectiveTo == null || item.EffectiveTo > now), cancellationToken);
        var hasRoleGrant = await db.UserRoleAssignments
            .AsNoTracking()
            .Where(assignment =>
                assignment.UserId == session.UserId &&
                assignment.Status == StatusKind.ACTIVE &&
                (organizationId == null || assignment.OrganizationId == organizationId) &&
                (organizationUnitId == null || assignment.OrganizationUnitId == null || assignment.OrganizationUnitId == organizationUnitId) &&
                assignment.Role.Status == StatusKind.ACTIVE &&
                (assignment.Role.ApplicationScope == "BOTH" || assignment.Role.ApplicationScope == session.Application.ToString()) &&
                assignment.Role.Permissions.Any(mapping => mapping.PermissionId == permission.Id && mapping.Permission.IsActive))
            .AnyAsync(cancellationToken);
        return hasGrant || hasRoleGrant;
    }

    public async Task<IReadOnlyList<AccessUserResponse>> GetUsersAsync(Session session, CancellationToken cancellationToken)
    {
        var organizationIds = await db.UserOrganizationMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == session.UserId && membership.Status == StatusKind.ACTIVE)
            .Select(membership => membership.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var assignedOrganizationIds = await db.UserRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == session.UserId && assignment.Status == StatusKind.ACTIVE)
            .Select(assignment => assignment.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        organizationIds = organizationIds.Concat(assignedOrganizationIds).Distinct().ToList();

        var users = await db.Users
            .AsNoTracking()
            .Include(user => user.ApplicationAccess)
            .Include(user => user.OrganizationMemberships)
                .ThenInclude(membership => membership.Organization)
            .Include(user => user.OrganizationMemberships)
                .ThenInclude(membership => membership.OrganizationUnit)
            .Include(user => user.RoleAssignments)
                .ThenInclude(assignment => assignment.Role)
                    .ThenInclude(role => role.Permissions)
                        .ThenInclude(mapping => mapping.Permission)
            .Include(user => user.RoleAssignments)
                .ThenInclude(assignment => assignment.Organization)
            .Include(user => user.RoleAssignments)
                .ThenInclude(assignment => assignment.OrganizationUnit)
            .Where(user => user.OrganizationMemberships.Any(membership =>
                membership.Status == StatusKind.ACTIVE && organizationIds.Contains(membership.OrganizationId)))
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        return users.Select(user => new AccessUserResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            user.Status,
            user.ApplicationAccess
                .Where(access => access.Status == StatusKind.ACTIVE)
                .Select(access => access.Application.ToString())
                .OrderBy(application => application)
                .ToList(),
            user.OrganizationMemberships
                .Where(membership => membership.Status == StatusKind.ACTIVE)
                .Select(membership => membership.Organization)
                .DistinctBy(organization => organization.Id)
                .Select(ToOrganization)
                .ToList(),
            user.OrganizationMemberships
                .Where(membership => membership.Status == StatusKind.ACTIVE && membership.OrganizationUnit is not null)
                .Select(membership => membership.OrganizationUnit!)
                .DistinctBy(unit => unit.Id)
                .Select(ToUnit)
                .ToList(),
            user.RoleAssignments
                .Where(assignment => assignment.Status == StatusKind.ACTIVE)
                .Select(assignment => assignment.Role)
                .DistinctBy(role => role.Id)
                .Select(ToRole)
                .ToList(),
            user.FirstName,
            user.LastName,
            user.MobileNumber,
            user.MustChangePassword)).ToList();
    }

    public async Task<AccessUserDetailResponse?> GetUserDetailAsync(Session session, Guid userId, CancellationToken cancellationToken)
    {
        var user = (await GetUsersAsync(session, cancellationToken)).SingleOrDefault(item => item.Id == userId);
        if (user is null) return null;

        var permissions = await db.Permissions.AsNoTracking().Where(permission => permission.IsActive)
            .OrderBy(permission => permission.Module).ThenBy(permission => permission.Key).ToListAsync(cancellationToken);
        var assignments = await db.UserRoleAssignments.AsNoTracking()
            .Where(assignment => assignment.UserId == userId && assignment.Status == StatusKind.ACTIVE)
            .Include(assignment => assignment.Role).ThenInclude(role => role.Permissions)
            .ToListAsync(cancellationToken);
        var overrides = await db.UserAuthorizationOverrides.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Include(item => item.Authorization)
            .OrderBy(item => item.Authorization.Key)
            .ToListAsync(cancellationToken);
        var rolePermissionIds = assignments.SelectMany(item => item.Role.Permissions.Select(mapping => mapping.PermissionId)).ToHashSet();
        var effective = permissions.Select(permission =>
        {
            var deny = overrides.Any(item => item.AuthorizationId == permission.Id && item.OverrideType == AuthorizationOverrideType.DENY);
            var grant = overrides.Any(item => item.AuthorizationId == permission.Id && item.OverrideType == AuthorizationOverrideType.GRANT);
            var roleGrant = rolePermissionIds.Contains(permission.Id);
            var decision = deny ? "DENY" : (grant || roleGrant ? "GRANT" : "NONE");
            var source = deny ? "USER_DENY" : (grant ? "USER_GRANT" : (roleGrant ? "ROLE" : "NONE"));
            return new AuthorizationPreviewResponse(permission.Id, permission.Key, permission.Name, permission.Module, permission.ApplicationScope, permission.RiskLevel, decision, source);
        }).Where(item => item.Decision != "NONE").ToList();
        var overridePreview = overrides.Select(item => new AuthorizationPreviewResponse(
            item.Authorization.Id, item.Authorization.Key, item.Authorization.Name, item.Authorization.Module,
            item.Authorization.ApplicationScope, item.Authorization.RiskLevel, item.OverrideType.ToString(), "USER_OVERRIDE")).ToList();
        var storage = await db.UserDocumentStorageAssignments.AsNoTracking()
            .Where(item => item.UserId == userId)
            .Include(item => item.StorageConnection)
            .Include(item => item.DocumentStorageDestination)
            .Select(item => new StorageAssignmentResponse(
                item.Provider, item.Status, item.StorageConnectionId, item.SiteIdentifier, item.DriveIdentifier,
                item.FolderIdentifier, item.DestinationUrl, item.ExternalTransferEnabled,
                item.StorageConnection == null ? null : item.StorageConnection.ConnectionStatus.ToString(),
                item.DocumentStorageDestinationId,
                item.DocumentStorageDestination == null ? null : item.DocumentStorageDestination.FolderPath))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new StorageAssignmentResponse(DocumentStorageProvider.NONE, StorageAssignmentStatus.NOT_CONFIGURED, null, null, null, null, null, false, null, null, null);
        return new AccessUserDetailResponse(user, effective, overridePreview, storage);
    }

    private async Task<AccessContextResponse> GetSupportContextAsync(Session session, CancellationToken cancellationToken)
    {
        var organizationRows = await db.Organizations.AsNoTracking()
            .Where(item => item.Status == StatusKind.ACTIVE)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var unitRows = await db.OrganizationUnits.AsNoTracking()
            .Where(item => item.Status == StatusKind.ACTIVE)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var keys = DelegatedPermissionKeys(session);
        var permissionRows = await db.Permissions.AsNoTracking()
            .Where(item => item.IsActive)
            .ToListAsync(cancellationToken);
        return new AccessContextResponse(
            userService.ToResponse(null, session.Application, session),
            organizationRows.Select(ToOrganization).ToList(),
            unitRows.Select(ToUnit).ToList(),
            [],
            permissionRows.Where(item => keys.Contains(item.Key)).OrderBy(item => item.Key).Select(ToPermission).ToList());
    }

    internal static HashSet<string> DelegatedPermissionKeys(Session session)
    {
        if (string.Equals(session.PlatformRole, "PLATFORM_SUPER_ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionKeys.All.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var assigned = (session.DelegatedPermissionsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (assigned.Length > 0)
        {
            return assigned.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var viewOnly = new[]
        {
            PermissionKeys.OrganizationRead, PermissionKeys.ViewInvoice, PermissionKeys.ViewPurchaseOrder,
            PermissionKeys.ViewGrn, PermissionKeys.ViewExtraction, PermissionKeys.ViewAuditLog, "VIEW_DASHBOARD",
        };
        if (string.Equals(session.PlatformRole, "CUSTOMER_SUPPORT_ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return viewOnly.Concat([
                PermissionKeys.EditInvoice, PermissionKeys.ViewGrn, PermissionKeys.CreateGrn, PermissionKeys.MatchPurchaseOrder,
            ]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return viewOnly.ToHashSet(StringComparer.OrdinalIgnoreCase);
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

    private static PermissionResponse ToPermission(Permission permission) =>
        new(permission.Id, permission.Key, permission.Name, permission.Description, permission.Module, permission.SubModule, permission.ApplicationScope, permission.RiskLevel, permission.IsSystemAuthorization, permission.IsActive);

    private static RoleResponse ToRole(Role role) =>
        new(role.Id, role.Key, role.Name, role.Description, role.IsSystem, role.Status,
            role.Permissions.Where(mapping => mapping.Permission.IsActive).Select(mapping => ToPermission(mapping.Permission)).OrderBy(permission => permission.Key).ToList(),
            role.ApplicationScope);
}