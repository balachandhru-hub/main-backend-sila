using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class RecipeUom
{
    public static readonly (string Code, string Name, UomDimension Dimension)[] Defaults =
    [
        ("EA", "Each", UomDimension.COUNT),
        ("PC", "Piece", UomDimension.COUNT),
        ("KG", "Kilogram", UomDimension.MASS),
        ("G", "Gram", UomDimension.MASS),
        ("L", "Litre", UomDimension.VOLUME),
        ("ML", "Millilitre", UomDimension.VOLUME),
        ("BOT", "Bottle", UomDimension.PACK),
        ("CAN", "Can", UomDimension.PACK),
        ("PACK", "Pack", UomDimension.PACK),
        ("BOX", "Box", UomDimension.PACK),
        ("TRAY", "Tray", UomDimension.PACK),
        ("PORTION", "Portion", UomDimension.SERVING),
        ("GLASS", "Glass", UomDimension.SERVING),
        ("SLICE", "Slice", UomDimension.SERVING),
        ("SCOOP", "Scoop", UomDimension.SERVING),
    ];

    public static string Normalize(string? value)
    {
        var key = RecipeCosting.NormalizeKey(value);
        return key switch
        {
            "KILO" or "KILOGRAM" or "KILOGRAMS" => "KG",
            "GRAM" or "GRAMS" => "G",
            "LITRE" or "LITER" or "LITRES" or "LITERS" or "LT" => "L",
            "MILLILITRE" or "MILLILITER" or "MLS" => "ML",
            "EACH" or "UNIT" or "UNITS" => "EA",
            "PIECE" or "PIECES" or "PCS" => "PC",
            "BOTTLE" or "BOTTLES" => "BOT",
            "CARTON" => "BOX",
            _ => key,
        };
    }

    public static string PackSummary(string baseUom, IEnumerable<MaterialUomConversion> conversions)
    {
        var baseCode = Normalize(baseUom);
        var match = conversions.Where(item => item.IsActive)
            .Select(item => new { From = Normalize(item.FromUom), To = Normalize(item.ToUom), item.Numerator, item.Denominator, item.PackSize, item.PackUom })
            .FirstOrDefault(item => item.From == baseCode && item.To != baseCode && item.Denominator != 0);
        if (match is null) return $"Base: {baseCode}";
        var pack = match.PackSize is > 0 ? $" · Pack {match.PackSize} {Normalize(match.PackUom) ?? match.To}" : string.Empty;
        return $"Base: {baseCode} · {Formula(baseCode, match.Denominator, match.To, match.Numerator)}{pack}";
    }

    public static string? Formula(string? baseUom, decimal? convFactor, string? convUnit, decimal? convValue)
    {
        if (string.IsNullOrWhiteSpace(convUnit) || convValue is not > 0) return null;
        var factor = convFactor is > 0 ? convFactor.Value : 1m;
        return $"{factor:0.####} {Normalize(baseUom)} = {convValue:0.####} {Normalize(convUnit)}";
    }

    public static IReadOnlyList<MaterialUomConversion> EffectiveConversions(Material material, IEnumerable<MaterialUomConversion>? conversions = null)
    {
        var list = (conversions ?? []).Where(item => item.IsActive).ToList();
        if (string.IsNullOrWhiteSpace(material.ConvUnit) || material.ConvValue is not > 0) return list;
        var from = Normalize(material.BaseUom);
        var to = Normalize(material.ConvUnit);
        if (from == to) return list;
        if (list.Any(item => Normalize(item.FromUom) == from && Normalize(item.ToUom) == to)) return list;
        list.Add(new MaterialUomConversion
        {
            Id = Guid.Empty, MaterialId = material.Id, FromUom = from, ToUom = to,
            Numerator = material.ConvValue.Value,
            Denominator = material.ConvFactor is > 0 ? material.ConvFactor.Value : 1m,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        return list;
    }

    public static (decimal Factor, string Unit, decimal Value)? Alternate(Material item, IEnumerable<MaterialUomConversion>? conversions = null)
    {
        if (!string.IsNullOrWhiteSpace(item.ConvUnit) && item.ConvValue is > 0)
            return (item.ConvFactor is > 0 ? item.ConvFactor.Value : 1m, Normalize(item.ConvUnit), item.ConvValue.Value);
        var baseCode = Normalize(item.BaseUom);
        var match = (conversions ?? []).FirstOrDefault(row =>
            row.IsActive && Normalize(row.FromUom) == baseCode && Normalize(row.ToUom) != baseCode && row.Denominator != 0);
        if (match is null) return null;
        return (match.Denominator, Normalize(match.ToUom), match.Numerator);
    }

    public static decimal? Convert(decimal quantity, string fromUom, string toUom, IReadOnlyList<MaterialUomConversion>? conversions)
    {
        var from = Normalize(fromUom);
        var to = Normalize(toUom);
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) return null;
        if (from == to) return quantity;
        var generic = RecipeCosting.ConvertQuantity(quantity, from, to);
        if (generic is not null) return generic;

        var edges = new Dictionary<string, List<(string To, decimal Factor)>>(StringComparer.OrdinalIgnoreCase);
        void Add(string a, string b, decimal factor)
        {
            if (!edges.TryGetValue(a, out var list))
            {
                list = [];
                edges[a] = list;
            }
            list.Add((b, factor));
        }

        foreach (var row in conversions ?? [])
        {
            if (!row.IsActive || row.Denominator == 0) continue;
            var a = Normalize(row.FromUom);
            var b = Normalize(row.ToUom);
            if (a == b) continue;
            var forward = row.Numerator / row.Denominator;
            if (forward == 0) continue;
            Add(a, b, forward);
            Add(b, a, 1m / forward);
        }
        Add("KG", "G", 1000m);
        Add("G", "KG", 0.001m);
        Add("L", "ML", 1000m);
        Add("ML", "L", 0.001m);

        var visited = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { [from] = 1m };
        var queue = new Queue<string>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!edges.TryGetValue(current, out var next)) continue;
            foreach (var (target, factor) in next)
            {
                if (visited.ContainsKey(target)) continue;
                visited[target] = visited[current] * factor;
                if (target == to)
                    return decimal.Round(quantity * visited[target], 6, MidpointRounding.AwayFromZero);
                queue.Enqueue(target);
            }
        }
        return null;
    }

    public static RecipeProfitability Profitability(decimal totalRecipeCost, decimal servingQty, decimal? menuPrice)
    {
        var divisor = servingQty > 0 ? servingQty : 1m;
        var costPerServing = decimal.Round(totalRecipeCost / divisor, 4, MidpointRounding.AwayFromZero);
        var costPercent = menuPrice is null or 0 ? (decimal?)null : decimal.Round(costPerServing / menuPrice.Value * 100m, 4, MidpointRounding.AwayFromZero);
        var margin = menuPrice is null ? (decimal?)null : decimal.Round(menuPrice.Value - costPerServing, 4, MidpointRounding.AwayFromZero);
        var marginPercent = menuPrice is null or 0 || margin is null
            ? (decimal?)null
            : decimal.Round(margin.Value / menuPrice.Value * 100m, 4, MidpointRounding.AwayFromZero);
        return new RecipeProfitability(totalRecipeCost, costPerServing, costPercent, margin, marginPercent);
    }
}

public sealed record RecipeProfitability(
    decimal RecipeCost, decimal CostPerServing, decimal? CostPercent, decimal? MarginAmount, decimal? MarginPercent);
