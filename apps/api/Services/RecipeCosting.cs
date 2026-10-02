using System.Globalization;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class RecipeCosting
{
    public static decimal EffectiveQuantity(decimal quantity, decimal wastagePercent, decimal yieldPercent)
    {
        var yieldFactor = yieldPercent <= 0 ? 1m : yieldPercent / 100m;
        return decimal.Round(quantity * (1m + wastagePercent / 100m) / yieldFactor, 6, MidpointRounding.AwayFromZero);
    }

    public static decimal IngredientCost(decimal quantity, decimal wastagePercent, decimal yieldPercent, decimal? unitCost) =>
        decimal.Round(EffectiveQuantity(quantity, wastagePercent, yieldPercent) * (unitCost ?? 0m), 4, MidpointRounding.AwayFromZero);

    public static decimal? LineCost(decimal quantity, decimal? unitPrice) =>
        unitPrice is null ? null : decimal.Round(quantity * unitPrice.Value, 4, MidpointRounding.AwayFromZero);

    public static (decimal Quantity, string Uom) TotalServing(IReadOnlyList<(decimal Quantity, string Uom, int Sequence)> ingredients)
    {
        if (ingredients.Count == 0) return (0, "EA");
        var ordered = ingredients.OrderBy(item => item.Sequence).ToList();
        var codes = ordered.Select(item => NormalizeUom(item.Uom)).Distinct().ToList();
        var primary = ordered[0];
        var targetUom = codes.Count == 1 ? primary.Uom : primary.Uom;
        var total = 0m;
        foreach (var item in ordered)
        {
            var converted = ConvertQuantity(item.Quantity, item.Uom, targetUom);
            if (converted is { } qty) total += qty;
        }
        return (decimal.Round(total, 6, MidpointRounding.AwayFromZero), targetUom);
    }

    public static decimal? QuantityOrCostShare(decimal quantity, string uom, decimal? lineCost, int sequence,
        IReadOnlyList<(decimal Quantity, string Uom, decimal? Cost, int Sequence)> all)
    {
        _ = quantity;
        _ = uom;
        _ = sequence;
        var costTotal = all.Sum(item => item.Cost ?? 0);
        return lineCost is null ? null : PercentageOfTotal(lineCost.Value, costTotal);
    }

    public static decimal? ConvertQuantity(decimal quantity, string fromUom, string toUom)
    {
        var from = NormalizeUom(fromUom);
        var to = NormalizeUom(toUom);
        if (from == to) return quantity;
        var fromDim = Dimension(from);
        var toDim = Dimension(to);
        if (fromDim == UomDimension.Unknown || fromDim != toDim) return null;
        return decimal.Round(ToCanonical(quantity, from) / ToCanonical(1, to), 6, MidpointRounding.AwayFromZero);
    }

    private enum UomDimension { Unknown, Mass, Volume, Count }

    private static UomDimension Dimension(string uom) => uom switch
    {
        "KG" or "G" => UomDimension.Mass,
        "L" or "ML" => UomDimension.Volume,
        "EA" or "EACH" or "PC" or "PCS" => UomDimension.Count,
        _ => UomDimension.Unknown,
    };

    private static decimal ToCanonical(decimal quantity, string uom) => uom switch
    {
        "G" or "ML" => quantity / 1000m,
        _ => quantity,
    };

    private static string NormalizeUom(string? value)
    {
        var key = NormalizeKey(value);
        return key switch
        {
            "KILO" or "KILOGRAM" or "KILOGRAMS" => "KG",
            "GRAM" or "GRAMS" => "G",
            "LITRE" or "LITER" or "LITRES" or "LITERS" or "LT" => "L",
            "MILLILITRE" or "MILLILITER" or "MLS" => "ML",
            "EACH" or "UNIT" or "UNITS" => "EA",
            "PIECE" or "PIECES" => "PC",
            _ => key,
        };
    }

    public static decimal RecipeTotalCost(IEnumerable<RecipeIngredient> ingredients) =>
        decimal.Round(ingredients.Sum(item => IngredientCost(item.Quantity, item.WastagePercent, item.YieldPercent, item.UnitCost)), 4, MidpointRounding.AwayFromZero);

    public static decimal CostPerPortion(decimal recipeTotalCost, decimal servingQuantity) =>
        servingQuantity == 0 ? 0 : decimal.Round(recipeTotalCost / servingQuantity, 4, MidpointRounding.AwayFromZero);

    public static decimal? PercentageOfTotal(decimal ingredientCost, decimal totalCost) =>
        totalCost == 0 ? null : decimal.Round(ingredientCost / totalCost * 100m, 4, MidpointRounding.AwayFromZero);

    public static decimal? PosItemCostPercentage(decimal totalPosItemCost, decimal? menuPrice) =>
        menuPrice is null or 0 ? null : decimal.Round(totalPosItemCost / menuPrice.Value * 100m, 4, MidpointRounding.AwayFromZero);

    public static decimal ConsumedQuantity(RecipeIngredient ingredient, decimal quantitySold, decimal servingQty = 1)
    {
        var perSale = ingredient.ConsumptionQuantity ?? EffectiveQuantity(ingredient.Quantity, ingredient.WastagePercent, ingredient.YieldPercent);
        var serving = servingQty == 0 ? 1 : servingQty;
        return decimal.Round(perSale / serving * quantitySold, 6, MidpointRounding.AwayFromZero);
    }

    public static string NormalizeKey(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}

public sealed class RecipeManagementException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}
