using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class RecipeReadinessEvaluator
{
    public static RecipeReadinessInfo Evaluate(
        IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyDictionary<Guid, Material> materials,
        IReadOnlyDictionary<Guid, IReadOnlyList<MaterialUomConversion>> conversions,
        IReadOnlyDictionary<Guid, MaterialChangeRequest> pending)
    {
        var issues = new List<RecipeReadinessIssue>();
        foreach (var line in ingredients.OrderBy(item => item.Sequence))
        {
            if (line.MaterialId is not { } materialId || !materials.TryGetValue(materialId, out var material))
            {
                issues.Add(new RecipeReadinessIssue(line.ErpMaterialId ?? "", line.MaterialDescription ?? "Ingredient", "MATERIAL_NOT_FOUND", $"{line.MaterialDescription ?? "Ingredient"} — Material not found"));
                continue;
            }
            if (material.Status != StatusKind.ACTIVE && material.GovernanceStatus is not (MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED))
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "MATERIAL_INACTIVE", $"{material.Description} — Material is not active"));
            pending.TryGetValue(material.Id, out var change);
            conversions.TryGetValue(material.Id, out var listed);
            var materialConversions = RecipeUom.EffectiveConversions(material, listed);
            var priceStatus = MaterialCosting.PriceStatus(material, change);
            if (priceStatus == "PRICE_MISSING")
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "PRICE_MISSING", $"{material.Description} — Price Missing"));
            else if (priceStatus == "PRICE_PENDING_APPROVAL" && !MaterialCosting.HasApprovedUnitPrice(material))
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "PRICE_PENDING_APPROVAL", $"{material.Description} — Material price pending approval"));
            else if (priceStatus is "PRICE_REJECTED" && !MaterialCosting.HasApprovedUnitPrice(material))
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "PRICE_REJECTED", $"{material.Description} — Price Rejected"));
            else if (priceStatus is "PRICE_INVALID" or "PRICE_EXPIRED")
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "PRICE_INVALID", $"{material.Description} — Price Invalid"));
            else if (!MaterialCosting.HasApprovedUnitPrice(material))
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "PRICE_MISSING", $"{material.Description} — Price Missing"));
            var consumption = RecipeUom.Convert(line.Quantity, line.Uom, material.BaseUom, materialConversions);
            if (consumption is null)
                issues.Add(new RecipeReadinessIssue(material.MaterialCode, material.Description, "UOM_CONVERSION_MISSING", $"{material.Description} — UOM conversion missing"));
        }
        var ready = issues.Count == 0 && ingredients.Count > 0;
        return new RecipeReadinessInfo(ready ? "READY_FOR_APPROVAL" : "NOT_READY", issues.Count, issues, ready);
    }
}
