using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class MaterialApprovalService(SilaMeDbContext db, RecipeApprovalService recipes, ApprovalWorkflowService workflows)
{
    public async Task<MaterialChangeRequest> SubmitAsync(
        Guid organizationId,
        Material material,
        MaterialNormalizedRecord proposed,
        RecipeApprovalEvent eventKind,
        MaterialAcquisitionSource source,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        await recipes.EnsureDefaultsAsync(organizationId, cancellationToken);
        var resolved = await workflows.ResolveLevelsAsync(organizationId, new ApprovalWorkflowMatchContext(
            ApprovalWorkflowType.MATERIAL_MASTER, ApprovalWorkflowFields.MapRecipeAction(eventKind),
            null, proposed.CompanyCode ?? material.CompanyCode, null, null,
            new Dictionary<string, decimal?>
            {
                ["UNIT_COST"] = proposed.UnitCost,
                ["STANDARD_PRICE"] = proposed.StandardPrice,
                ["MOVING_AVERAGE_PRICE"] = proposed.MovingAveragePrice,
            }), cancellationToken);
        var levels = resolved ?? (await db.RecipeApprovalWorkflows.Include(item => item.Levels)
            .SingleAsync(item => item.OrganizationId == organizationId && item.Event == eventKind, cancellationToken))
            .Levels.OrderBy(item => item.Level).Select(item => (item.Level, item.RoleKey, item.Label)).ToList();
        var now = DateTime.UtcNow;
        var request = new MaterialChangeRequest
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialId = material.Id, MaterialCode = material.MaterialCode,
            Event = eventKind, Source = source, ProposedJson = MaterialNormalized.Serialize(proposed),
            Status = MaterialGovernanceStatus.PENDING_APPROVAL, SubmittedByUserId = userId, SubmittedAt = now,
        };
        foreach (var level in levels)
        {
            request.Actions.Add(new MaterialApprovalAction
            {
                Id = Guid.NewGuid(), MaterialId = material.Id, Event = eventKind, Level = level.Level,
                RoleKey = level.RoleKey, Status = RecipeApprovalActionStatus.PENDING, SubmittedAt = now,
            });
        }
        db.MaterialChangeRequests.Add(request);
        material.PendingChangeRequestId = request.Id;
        material.GovernanceStatus = eventKind == RecipeApprovalEvent.CREATE_MATERIAL
            ? MaterialGovernanceStatus.PENDING_APPROVAL
            : material.GovernanceStatus;
        if (eventKind == RecipeApprovalEvent.CREATE_MATERIAL)
            material.Status = StatusKind.INACTIVE;
        await db.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<IReadOnlyList<MaterialApprovalTaskRow>> ListAsync(Guid organizationId, CancellationToken cancellationToken) =>
        await db.MaterialApprovalActions.AsNoTracking()
            .Where(item => item.ChangeRequest.OrganizationId == organizationId)
            .OrderByDescending(item => item.SubmittedAt)
            .Select(item => new MaterialApprovalTaskRow(item.Id, item.ChangeRequestId, item.MaterialId, item.ChangeRequest.MaterialCode,
                item.Event.ToString(), item.ChangeRequest.Source.ToString(), item.Level, item.RoleKey, item.Status.ToString(),
                item.Comment, item.SubmittedAt, item.ActionAt))
            .Take(200)
            .ToListAsync(cancellationToken);

    public async Task DecideAsync(Session session, Guid organizationId, Guid actionId, bool approve, RecipeApprovalDecisionRequest request, CancellationToken cancellationToken)
    {
        var action = await db.MaterialApprovalActions.Include(item => item.ChangeRequest)
            .SingleOrDefaultAsync(item => item.Id == actionId && item.ChangeRequest.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("APPROVAL_NOT_FOUND", "The material approval task was not found.", 404);
        if (action.Status != RecipeApprovalActionStatus.PENDING)
            throw new RecipeManagementException("APPROVAL_ALREADY_DECISIONED", "This approval step is already complete.");
        var now = DateTime.UtcNow;
        action.Status = approve ? RecipeApprovalActionStatus.APPROVED : RecipeApprovalActionStatus.REJECTED;
        action.Comment = request.Comment;
        action.ApproverUserId = session.CustomerUserId();
        action.ActionAt = now;
        var change = action.ChangeRequest;
        var material = change.MaterialId is { } id
            ? await db.Materials.SingleAsync(item => item.Id == id, cancellationToken)
            : throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        if (!approve)
        {
            change.Status = MaterialGovernanceStatus.REJECTED;
            change.CompletedAt = now;
            material.PendingChangeRequestId = material.PendingChangeRequestId == change.Id ? null : material.PendingChangeRequestId;
            if (change.Event == RecipeApprovalEvent.CREATE_MATERIAL)
            {
                material.GovernanceStatus = MaterialGovernanceStatus.REJECTED;
                material.Status = StatusKind.INACTIVE;
            }
            foreach (var pending in await db.MaterialApprovalActions.Where(item => item.ChangeRequestId == change.Id && item.Status == RecipeApprovalActionStatus.PENDING).ToListAsync(cancellationToken))
                pending.Status = RecipeApprovalActionStatus.REJECTED;
        }
        else
        {
            var pending = await db.MaterialApprovalActions.AnyAsync(item => item.ChangeRequestId == change.Id && item.Status == RecipeApprovalActionStatus.PENDING && item.Id != action.Id, cancellationToken);
            if (!pending)
            {
                var proposed = MaterialNormalized.Deserialize(change.ProposedJson);
                MaterialNormalized.Apply(material, proposed, operational: true);
                await ReplaceValuationsAsync(material.Id, proposed, cancellationToken);
                await MaterialMasterService.SyncAlternateConversionAsync(db, material, proposed.Source.ToString(), cancellationToken);
                material.GovernanceStatus = MaterialGovernanceStatus.ACTIVE;
                material.Status = StatusKind.ACTIVE;
                material.PendingChangeRequestId = null;
                change.Status = MaterialGovernanceStatus.APPROVED;
                change.CompletedAt = now;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task ReplaceValuationsAsync(SilaMeDbContext db, Guid materialId, MaterialNormalizedRecord proposed, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var rows = await db.MaterialValuations.Where(item => item.MaterialId == materialId).ToListAsync(cancellationToken);
        var current = rows.Where(item => item.EffectiveTo is null).ToList();
        foreach (var valuation in proposed.Valuations ?? [])
        {
            if (string.IsNullOrWhiteSpace(valuation.ValuationArea)) continue;
            var area = valuation.ValuationArea.Trim();
            var existing = current.FirstOrDefault(item => string.Equals(item.ValuationArea, area, StringComparison.OrdinalIgnoreCase));
            var effectiveFrom = valuation.EffectiveFrom ?? now;
            var same = existing is not null
                && existing.StandardPrice == valuation.StandardPrice
                && existing.MovingAveragePrice == valuation.MovingAveragePrice
                && string.Equals(existing.Currency ?? "", valuation.Currency ?? "", StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.PriceControl ?? "", valuation.PriceControl ?? "", StringComparison.OrdinalIgnoreCase)
                && string.Equals(existing.PriceUom ?? "", valuation.PriceUom ?? "", StringComparison.OrdinalIgnoreCase);
            if (same && existing is not null)
            {
                existing.CompanyCode = valuation.CompanyCode;
                existing.Plant = valuation.Plant ?? existing.Plant;
                existing.ValuationClass = valuation.ValuationClass;
                existing.UpdatedAt = now;
                continue;
            }
            if (existing is not null)
                existing.EffectiveTo = effectiveFrom;
            db.MaterialValuations.Add(new MaterialValuation
            {
                Id = Guid.NewGuid(), MaterialId = materialId, CompanyCode = valuation.CompanyCode,
                ValuationArea = area, Plant = valuation.Plant, ValuationClass = valuation.ValuationClass,
                PriceControl = valuation.PriceControl, StandardPrice = valuation.StandardPrice,
                MovingAveragePrice = valuation.MovingAveragePrice, Currency = valuation.Currency,
                PriceUom = valuation.PriceUom, EffectiveFrom = effectiveFrom, EffectiveTo = null,
                Source = proposed.SourceSystem ?? proposed.Source.ToString(), UpdatedAt = now,
            });
        }
    }

    private Task ReplaceValuationsAsync(Guid materialId, MaterialNormalizedRecord proposed, CancellationToken cancellationToken) =>
        ReplaceValuationsAsync(db, materialId, proposed, cancellationToken);
}
