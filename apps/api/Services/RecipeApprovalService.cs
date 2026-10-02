using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class RecipeApprovalService(SilaMeDbContext db, ApprovalWorkflowService workflows)
{
    public async Task<IReadOnlyList<RecipeApprovalWorkflowRow>> ListWorkflowsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(organizationId, cancellationToken);
        return await db.RecipeApprovalWorkflows.AsNoTracking().Include(item => item.Levels)
            .Where(item => item.OrganizationId == organizationId)
            .Select(item => new RecipeApprovalWorkflowRow(
                item.Id, item.Event.ToString(), item.LevelCount,
                item.Levels.OrderBy(level => level.Level).Select(level => new RecipeApprovalLevelInput(level.Level, level.RoleKey, level.Label)).ToList()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecipeApprovalWorkflowRow> UpsertWorkflowAsync(Guid organizationId, RecipeApprovalWorkflowUpsertRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RecipeApprovalEvent>(request.Event, true, out var eventKind))
            throw new RecipeManagementException("APPROVAL_EVENT_INVALID", "Use CREATE_RECIPE, CHANGE_RECIPE, CREATE_MATERIAL, or CHANGE_MATERIAL.");
        if (request.Levels.Count == 0)
            throw new RecipeManagementException("APPROVAL_LEVELS_REQUIRED", "Define at least one approval level.");
        var now = DateTime.UtcNow;
        var workflow = await db.RecipeApprovalWorkflows.Include(item => item.Levels)
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Event == eventKind, cancellationToken);
        if (workflow is null)
        {
            workflow = new RecipeApprovalWorkflow { Id = Guid.NewGuid(), OrganizationId = organizationId, Event = eventKind, CreatedAt = now };
            db.RecipeApprovalWorkflows.Add(workflow);
        }
        db.RecipeApprovalLevels.RemoveRange(workflow.Levels);
        workflow.LevelCount = request.Levels.Count;
        workflow.UpdatedAt = now;
        foreach (var level in request.Levels.OrderBy(item => item.Level))
        {
            workflow.Levels.Add(new RecipeApprovalLevel
            {
                Id = Guid.NewGuid(), WorkflowId = workflow.Id, Level = level.Level,
                RoleKey = level.RoleKey.Trim().ToUpperInvariant(), Label = level.Label.Trim(),
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return (await ListWorkflowsAsync(organizationId, cancellationToken)).Single(item => item.Event == eventKind.ToString());
    }

    public async Task SubmitAsync(Guid organizationId, Guid recipeId, Guid versionId, RecipeApprovalEvent eventKind, CancellationToken cancellationToken)
    {
        await EnsureDefaultsAsync(organizationId, cancellationToken);
        var recipe = await db.Recipes.SingleAsync(item => item.Id == recipeId, cancellationToken);
        var version = await db.RecipeVersions.Include(item => item.Ingredients).SingleAsync(item => item.Id == versionId, cancellationToken);
        var total = RecipeCosting.RecipeTotalCost(version.Ingredients);
        var resolved = await workflows.ResolveLevelsAsync(organizationId, new ApprovalWorkflowMatchContext(
            ApprovalWorkflowType.RECIPE, ApprovalWorkflowFields.MapRecipeAction(eventKind),
            null, null, null, null,
            new Dictionary<string, decimal?>
            {
                ["RECIPE_TOTAL_COST"] = total,
                ["COST_PER_PORTION"] = RecipeCosting.CostPerPortion(total, recipe.ServingSize ?? 1),
                ["SERVING_QUANTITY"] = recipe.ServingSize ?? 1,
                ["INGREDIENT_COUNT"] = version.Ingredients.Count,
                ["PREPARATION_MINUTES"] = version.PreparationMinutes,
            }), cancellationToken);
        var levels = resolved ?? (await db.RecipeApprovalWorkflows.Include(item => item.Levels)
            .SingleAsync(item => item.OrganizationId == organizationId && item.Event == eventKind, cancellationToken))
            .Levels.OrderBy(item => item.Level).Select(item => (item.Level, item.RoleKey, item.Label)).ToList();
        var now = DateTime.UtcNow;
        recipe.Status = RecipeStatus.PENDING_APPROVAL;
        version.Status = RecipeStatus.PENDING_APPROVAL;
        version.UpdatedAt = now;
        foreach (var level in levels)
        {
            db.RecipeApprovalActions.Add(new RecipeApprovalAction
            {
                Id = Guid.NewGuid(), RecipeId = recipe.Id, RecipeVersionId = version.Id, Event = eventKind,
                Level = level.Level, RoleKey = level.RoleKey, Status = RecipeApprovalActionStatus.PENDING, SubmittedAt = now,
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeApprovalActionRow>> PendingAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await db.RecipeApprovalActions.AsNoTracking()
            .Where(item => item.Recipe.OrganizationId == organizationId)
            .OrderByDescending(item => item.SubmittedAt)
            .Select(item => new RecipeApprovalActionRow(item.Id, item.RecipeId, item.RecipeVersionId, item.RecipeVersion.VersionNumber,
                item.Recipe.Name, item.Event.ToString(), item.Level, item.RoleKey, item.Status.ToString(), item.Comment, item.SubmittedAt, item.ActionAt))
            .Take(200)
            .ToListAsync(cancellationToken);

    public async Task DecideAsync(Session session, Guid organizationId, Guid actionId, bool approve, RecipeApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        var action = await db.RecipeApprovalActions.Include(item => item.Recipe).Include(item => item.RecipeVersion)
            .SingleOrDefaultAsync(item => item.Id == actionId && item.Recipe.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("APPROVAL_NOT_FOUND", "The approval task was not found.", 404);
        if (action.Status != RecipeApprovalActionStatus.PENDING)
            throw new RecipeManagementException("APPROVAL_ALREADY_DECISIONED", "This approval step is already complete.");
        var now = DateTime.UtcNow;
        action.Status = approve ? RecipeApprovalActionStatus.APPROVED : RecipeApprovalActionStatus.REJECTED;
        action.Comment = request.Comment;
        action.ApproverUserId = session.CustomerUserId();
        action.ActionAt = now;
        if (!approve)
        {
            action.Recipe.Status = RecipeStatus.REJECTED;
            action.RecipeVersion.Status = RecipeStatus.REJECTED;
            var remaining = await db.RecipeApprovalActions.Where(item => item.RecipeVersionId == action.RecipeVersionId && item.Status == RecipeApprovalActionStatus.PENDING).ToListAsync(cancellationToken);
            foreach (var pending in remaining) pending.Status = RecipeApprovalActionStatus.REJECTED;
        }
        else
        {
            var pending = await db.RecipeApprovalActions.AnyAsync(item => item.RecipeVersionId == action.RecipeVersionId && item.Status == RecipeApprovalActionStatus.PENDING && item.Id != action.Id, cancellationToken);
            if (!pending)
                await ActivateAsync(action.Recipe, action.RecipeVersion, session.CustomerUserId(), now, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ActivateAsync(Recipe recipe, RecipeVersion version, Guid approverId, DateTime now, CancellationToken cancellationToken)
    {
        var loaded = await db.RecipeVersions.Include(item => item.Ingredients).ThenInclude(item => item.Material)
            .SingleAsync(item => item.Id == version.Id, cancellationToken);
        var materialIds = loaded.Ingredients.Where(item => item.MaterialId is not null).Select(item => item.MaterialId!.Value).Distinct().ToList();
        var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => materialIds.Contains(item.MaterialId) && item.IsActive).ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking()
            .Where(item => item.Status == MaterialGovernanceStatus.PENDING_APPROVAL && item.MaterialId != null && materialIds.Contains(item.MaterialId.Value))
            .ToListAsync(cancellationToken);
        var materials = loaded.Ingredients.Where(item => item.Material is not null).Select(item => item.Material!).GroupBy(item => item.Id).ToDictionary(item => item.Key, item => item.First());
        var readiness = RecipeReadinessEvaluator.Evaluate(loaded.Ingredients.ToList(), materials,
            conversions.GroupBy(item => item.MaterialId).ToDictionary(item => item.Key, item => (IReadOnlyList<MaterialUomConversion>)item.ToList()),
            pending.Where(item => item.MaterialId is not null).GroupBy(item => item.MaterialId!.Value).ToDictionary(item => item.Key, item => item.First()));
        if (loaded.Ingredients.Count == 0 || !readiness.ReadyForApproval)
            throw new RecipeManagementException("RECIPE_NOT_READY",
                "This recipe cannot become ACTIVE until every ingredient has an approved Material price and a valid UOM conversion. " +
                string.Join("; ", readiness.Issues.Select(item => item.Message)));
        decimal total = 0;
        foreach (var item in loaded.Ingredients)
        {
            if (item.MaterialId is null || !materials.TryGetValue(item.MaterialId.Value, out var material)) continue;
            conversions.GroupBy(row => row.MaterialId).ToDictionary(row => row.Key, row => row.ToList()).TryGetValue(material.Id, out var materialConversions);
            var consumption = RecipeUom.Convert(item.Quantity, item.Uom, material.BaseUom, materialConversions ?? []);
            total += RecipeCosting.LineCost(consumption ?? 0, MaterialCosting.EffectiveUnitCost(material)) ?? 0;
        }
        total = decimal.Round(total, 4, MidpointRounding.AwayFromZero);
        var profit = RecipeUom.Profitability(total, recipe.ServingSize ?? 1, recipe.PosItemMenuPrice);
        version.SnapshotMenuPrice = recipe.PosItemMenuPrice;
        version.SnapshotTotalRecipeCost = profit.RecipeCost;
        version.SnapshotCostPerServing = profit.CostPerServing;
        version.SnapshotCostPercent = profit.CostPercent;
        version.SnapshotMarginAmount = profit.MarginAmount;
        version.SnapshotMarginPercent = profit.MarginPercent;
        version.SnapshotCalculatedAt = now;
        var previous = await db.RecipeVersions.Where(item => item.RecipeId == recipe.Id && item.Id != version.Id && item.Status == RecipeStatus.ACTIVE).ToListAsync(cancellationToken);
        foreach (var item in previous)
        {
            item.Status = RecipeStatus.APPROVED;
            item.EffectiveTo = now;
            item.UpdatedAt = now;
        }
        version.Status = RecipeStatus.ACTIVE;
        version.ApprovedByUserId = approverId;
        version.ApprovedAt = now;
        version.EffectiveFrom = now;
        version.EffectiveTo = null;
        version.UpdatedAt = now;
        recipe.Status = RecipeStatus.ACTIVE;
        recipe.ActiveVersionId = version.Id;
        recipe.UpdatedAt = now;
        recipe.UpdatedByUserId = approverId;
    }

    public async Task EnsureDefaultsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        foreach (var eventKind in new[]
                 {
                     RecipeApprovalEvent.CREATE_RECIPE, RecipeApprovalEvent.CHANGE_RECIPE,
                     RecipeApprovalEvent.CREATE_MATERIAL, RecipeApprovalEvent.CHANGE_MATERIAL
                 })
        {
            if (await db.RecipeApprovalWorkflows.AnyAsync(item => item.OrganizationId == organizationId && item.Event == eventKind, cancellationToken))
                continue;
            var now = DateTime.UtcNow;
            var workflow = new RecipeApprovalWorkflow
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, Event = eventKind, CreatedAt = now, UpdatedAt = now,
            };
            if (eventKind == RecipeApprovalEvent.CREATE_RECIPE)
            {
                workflow.LevelCount = 3;
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 1, RoleKey = "MENU_MANAGER", Label = "Executive Chef" });
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 2, RoleKey = "OPERATIONS_MANAGER", Label = "F&B Manager" });
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 3, RoleKey = "FINANCE_REVIEWER", Label = "Finance / Cost Controller" });
            }
            else if (eventKind == RecipeApprovalEvent.CHANGE_RECIPE)
            {
                workflow.LevelCount = 2;
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 1, RoleKey = "MENU_MANAGER", Label = "Executive Chef" });
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 2, RoleKey = "FINANCE_REVIEWER", Label = "Cost Controller" });
            }
            else if (eventKind == RecipeApprovalEvent.CREATE_MATERIAL)
            {
                workflow.LevelCount = 2;
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 1, RoleKey = "MENU_MANAGER", Label = "Material / Master Data Owner" });
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 2, RoleKey = "FINANCE_REVIEWER", Label = "Cost Controller" });
            }
            else
            {
                workflow.LevelCount = 1;
                workflow.Levels.Add(new RecipeApprovalLevel { Id = Guid.NewGuid(), Level = 1, RoleKey = "MENU_MANAGER", Label = "Material / Master Data Owner" });
            }
            db.RecipeApprovalWorkflows.Add(workflow);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
