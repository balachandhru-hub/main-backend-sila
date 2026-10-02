using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class ApprovalWorkflowService(SilaMeDbContext db)
{
    public ApprovalWorkflowCatalog Catalog(IReadOnlyList<ApprovalRoleOption> roles) =>
        new(ApprovalWorkflowFields.Types, ["ALL", "CREATE", "CHANGE"],
            ["ALL", "PROPERTY", "COMPANY_CODE", "OUTLET", "STORE"],
            ["GREATER_THAN", "LESS_THAN"], roles);

    public async Task<IReadOnlyList<ApprovalRoleOption>> RolesAsync(CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().Where(item => item.Status == StatusKind.ACTIVE)
            .OrderBy(item => item.Name)
            .Select(item => new ApprovalRoleOption(item.Key, item.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ApprovalScopeOption>> ScopeOptionsAsync(Guid organizationId, ApprovalScopeKind kind, CancellationToken cancellationToken)
    {
        if (kind == ApprovalScopeKind.PROPERTY)
            return await db.Properties.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && !item.IsDeleted && item.Status == StatusKind.ACTIVE)
                .OrderBy(item => item.PropertyCode)
                .Select(item => new ApprovalScopeOption(item.PropertyCode, item.PropertyName))
                .ToListAsync(cancellationToken);
        if (kind == ApprovalScopeKind.COMPANY_CODE)
            return await db.CompanyCodes.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && !item.IsDeleted && item.Status == StatusKind.ACTIVE)
                .OrderBy(item => item.CompanyCode)
                .Select(item => new ApprovalScopeOption(item.CompanyCode, item.CompanyName))
                .ToListAsync(cancellationToken);
        if (kind == ApprovalScopeKind.STORE)
            return await db.InventoryLocations.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE && item.LocationType == InventoryLocationType.STORE)
                .OrderBy(item => item.LocationCode)
                .Select(item => new ApprovalScopeOption(item.LocationCode, item.LocationName))
                .ToListAsync(cancellationToken);
        if (kind != ApprovalScopeKind.OUTLET) return [];
        return await db.InventoryLocations.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE && item.LocationType == InventoryLocationType.OUTLET)
            .OrderBy(item => item.LocationCode)
            .Select(item => new ApprovalScopeOption(item.LocationCode, item.LocationName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalWorkflowConfigurationRow>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(organizationId, cancellationToken);
        var rows = await db.ApprovalWorkflowConfigurations.AsNoTracking()
            .Include(item => item.Levels).Include(item => item.Conditions)
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.ApprovalType).ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRow).ToList();
    }

    public async Task<ApprovalWorkflowConfigurationRow> UpsertAsync(Guid organizationId, Guid? id, ApprovalWorkflowUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApprovalWorkflowType>(request.ApprovalType, true, out var type))
            throw new RecipeManagementException("APPROVAL_TYPE_INVALID", "Use RECIPE, MATERIAL_MASTER, SUPPLIER_MASTER, INTERNAL_TRANSFER, GRN, or INVOICE.");
        if (!Enum.TryParse<ApprovalWorkflowAction>(request.Action, true, out var action))
            throw new RecipeManagementException("APPROVAL_ACTION_INVALID", "Use ALL, CREATE, or CHANGE.");
        if (!Enum.TryParse<ApprovalScopeKind>(request.ScopeKind, true, out var scope))
            throw new RecipeManagementException("APPROVAL_SCOPE_INVALID", "Use ALL, PROPERTY, COMPANY_CODE, OUTLET, or STORE.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new RecipeManagementException("APPROVAL_NAME_REQUIRED", "Give this workflow configuration a name.");
        if (request.Levels.Count == 0)
            throw new RecipeManagementException("APPROVAL_LEVELS_REQUIRED", "Define at least Level 1, then add more levels as needed.");
        if (scope != ApprovalScopeKind.ALL && string.IsNullOrWhiteSpace(request.ScopeValue))
            throw new RecipeManagementException("APPROVAL_SCOPE_VALUE_REQUIRED", "Select a Property, Company Code, Outlet, or Store.");
        foreach (var condition in request.Conditions)
        {
            if (!Enum.TryParse<ApprovalConditionOperator>(condition.Operator, true, out _))
                throw new RecipeManagementException("APPROVAL_OPERATOR_INVALID", "Use GREATER_THAN or LESS_THAN.");
            if (!ApprovalWorkflowFields.IsKnownField(type, condition.FieldKey))
                throw new RecipeManagementException("APPROVAL_FIELD_INVALID", $"Field {condition.FieldKey} is not available for {type}.");
        }
        var now = DateTime.UtcNow;
        var row = id is null
            ? null
            : await db.ApprovalWorkflowConfigurations.Include(item => item.Levels).Include(item => item.Conditions)
                .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && row is null)
            throw new RecipeManagementException("APPROVAL_WORKFLOW_NOT_FOUND", "The workflow configuration was not found.", 404);
        row ??= new ApprovalWorkflowConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Name = request.Name.Trim(), CreatedAt = now,
        };
        if (id is null) db.ApprovalWorkflowConfigurations.Add(row);
        db.ApprovalWorkflowLevels.RemoveRange(row.Levels);
        db.ApprovalWorkflowConditions.RemoveRange(row.Conditions);
        row.Name = request.Name.Trim();
        row.ApprovalType = type;
        row.Action = action;
        row.ScopeKind = scope;
        row.ScopeValue = scope == ApprovalScopeKind.ALL ? null : request.ScopeValue!.Trim().ToUpperInvariant();
        row.IsActive = request.IsActive;
        row.UpdatedAt = now;
        var levelNumber = 1;
        foreach (var level in request.Levels.OrderBy(item => item.Level))
        {
            if (string.IsNullOrWhiteSpace(level.RoleKey))
                throw new RecipeManagementException("APPROVAL_ROLE_REQUIRED", "Each level needs an approver role.");
            row.Levels.Add(new ApprovalWorkflowLevel
            {
                Id = Guid.NewGuid(), ConfigurationId = row.Id, Level = levelNumber,
                RoleKey = level.RoleKey.Trim().ToUpperInvariant(),
                Label = string.IsNullOrWhiteSpace(level.Label) ? $"Level {levelNumber}" : level.Label.Trim(),
            });
            levelNumber++;
        }
        foreach (var condition in request.Conditions)
        {
            row.Conditions.Add(new ApprovalWorkflowCondition
            {
                Id = Guid.NewGuid(), ConfigurationId = row.Id,
                FieldKey = condition.FieldKey.Trim().ToUpperInvariant(),
                Operator = Enum.Parse<ApprovalConditionOperator>(condition.Operator, true),
                Value = condition.Value,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        await SyncLegacyAsync(organizationId, row, cancellationToken);
        return ToRow(row);
    }

    public async Task DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var row = await db.ApprovalWorkflowConfigurations.Include(item => item.Levels).Include(item => item.Conditions)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("APPROVAL_WORKFLOW_NOT_FOUND", "The workflow configuration was not found.", 404);
        db.ApprovalWorkflowConfigurations.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(int Level, string RoleKey, string Label)>?> ResolveLevelsAsync(
        Guid organizationId,
        ApprovalWorkflowMatchContext context,
        CancellationToken cancellationToken)
    {
        var rows = await db.ApprovalWorkflowConfigurations.AsNoTracking()
            .Include(item => item.Levels).Include(item => item.Conditions)
            .Where(item => item.OrganizationId == organizationId && item.IsActive && item.ApprovalType == context.ApprovalType)
            .ToListAsync(cancellationToken);
        var match = ApprovalWorkflowResolver.Resolve(rows, context);
        return match is null ? null : ApprovalWorkflowResolver.Levels(match);
    }

    private async Task EnsureSeededAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (await db.ApprovalWorkflowConfigurations.AnyAsync(item => item.OrganizationId == organizationId, cancellationToken))
            return;
        var legacy = await db.RecipeApprovalWorkflows.Include(item => item.Levels)
            .Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var workflow in legacy)
        {
            var type = ApprovalWorkflowFields.MapRecipeEvent(workflow.Event);
            if (type is null) continue;
            var config = new ApprovalWorkflowConfiguration
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId,
                Name = workflow.Event.ToString().Replace('_', ' '),
                ApprovalType = type.Value, Action = ApprovalWorkflowFields.MapRecipeAction(workflow.Event),
                ScopeKind = ApprovalScopeKind.ALL, IsActive = true, CreatedAt = now, UpdatedAt = now,
            };
            foreach (var level in workflow.Levels.OrderBy(item => item.Level))
            {
                config.Levels.Add(new ApprovalWorkflowLevel
                {
                    Id = Guid.NewGuid(), Level = level.Level, RoleKey = level.RoleKey, Label = level.Label,
                });
            }
            db.ApprovalWorkflowConfigurations.Add(config);
        }
        if (legacy.Count > 0) await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncLegacyAsync(Guid organizationId, ApprovalWorkflowConfiguration row, CancellationToken cancellationToken)
    {
        if (row.ScopeKind != ApprovalScopeKind.ALL || row.Conditions.Count > 0) return;
        var eventKind = ApprovalWorkflowFields.MapLegacyEvent(row.ApprovalType, row.Action);
        if (eventKind is null) return;
        var workflow = await db.RecipeApprovalWorkflows.Include(item => item.Levels)
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Event == eventKind, cancellationToken);
        var now = DateTime.UtcNow;
        if (workflow is null)
        {
            workflow = new RecipeApprovalWorkflow { Id = Guid.NewGuid(), OrganizationId = organizationId, Event = eventKind.Value, CreatedAt = now };
            db.RecipeApprovalWorkflows.Add(workflow);
        }
        db.RecipeApprovalLevels.RemoveRange(workflow.Levels);
        workflow.LevelCount = row.Levels.Count;
        workflow.UpdatedAt = now;
        foreach (var level in row.Levels.OrderBy(item => item.Level))
        {
            workflow.Levels.Add(new RecipeApprovalLevel
            {
                Id = Guid.NewGuid(), WorkflowId = workflow.Id, Level = level.Level, RoleKey = level.RoleKey, Label = level.Label,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static ApprovalWorkflowConfigurationRow ToRow(ApprovalWorkflowConfiguration item) =>
        new(item.Id, item.Name, item.ApprovalType.ToString(), item.Action.ToString(), item.ScopeKind.ToString(),
            item.ScopeValue, item.IsActive,
            item.Conditions.Select(condition => new ApprovalWorkflowConditionInput(condition.FieldKey, condition.Operator.ToString(), condition.Value)).ToList(),
            item.Levels.OrderBy(level => level.Level).Select(level => new ApprovalWorkflowLevelInput(level.Level, level.RoleKey, level.Label)).ToList(),
            item.UpdatedAt);
}
