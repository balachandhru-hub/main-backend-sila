using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class InventoryControlMath
{
    public const string Healthy = "HEALTHY";
    public const string Low = "LOW";
    public const string Out = "OUT";
    public const string Excess = "EXCESS";

    public static string Classify(decimal availableQty, decimal? reorderPoint, decimal? maximumStock)
    {
        if (availableQty <= 0) return Out;
        if (reorderPoint is decimal reorder && availableQty <= reorder) return Low;
        if (maximumStock is decimal max && availableQty > max) return Excess;
        return Healthy;
    }

    public static decimal? RecommendedQty(decimal availableQty, decimal? reorderPoint, decimal? parLevel)
    {
        if (reorderPoint is null || availableQty > reorderPoint.Value) return null;
        if (parLevel is null) return null;
        return Math.Max(0, parLevel.Value - availableQty);
    }

    public static decimal TransferableQty(decimal sourceAvailable, decimal? safetyStock, decimal? minimumStock)
    {
        var hold = Math.Max(safetyStock ?? 0, minimumStock ?? 0);
        return Math.Max(0, sourceAvailable - hold);
    }

    public static string CompactMoney(decimal value, string currency)
    {
        var abs = Math.Abs(value);
        var body = abs >= 1_000_000_000m
            ? $"{value / 1_000_000_000m:0.#}B"
            : abs >= 1_000_000m
                ? $"{value / 1_000_000m:0.#}M"
                : abs >= 10_000m
                    ? $"{value / 1_000m:0.#}K"
                    : value.ToString("N0");
        return $"{currency} {body}".Trim();
    }
}
