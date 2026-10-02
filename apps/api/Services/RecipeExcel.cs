using System.Globalization;
using SilaMe.Api.DTOs;

namespace SilaMe.Api.Services;

public static class RecipeExcel
{
    public static readonly string[] TemplateColumns =
    [
        "POS Code", "Title", "POS Item", "ServingUOM", "Serving Size", "RecipeID", "RecipeIngredientID", "Family", "Category",
        "Last Sale Date", "POS Item Menu price", "Ingredient Cost", "Total POS Item Cost", "PercentageofTotalCost",
        "POS Item Cost Percentage", "ERPMaterialID", "Status", "Material Group"
    ];

    public static readonly string[] MenuItemColumns =
    [
        "ItemCode", "Title", "Family", "Category", "ItemMode", "ServingQty", "ServingUOM", "MenuPrice", "Currency"
    ];

    public static readonly string[] LocationColumns =
    [
        "Kind", "Code", "Name", "Description", "Active"
    ];

    public static readonly string[] RecipeIngredientColumns =
    [
        "ItemCode", "Sequence", "MaterialID", "RecipeQty", "RecipeUOM"
    ];

    public static string? Get(IReadOnlyDictionary<string, string?> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            foreach (var pair in values)
            {
                if (Normalize(pair.Key) == Normalize(key) && !string.IsNullOrWhiteSpace(pair.Value))
                    return pair.Value.Trim();
            }
        }
        return null;
    }

    public static decimal? Decimal(IReadOnlyDictionary<string, string?> values, params string[] keys) =>
        decimal.TryParse(Get(values, keys), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

    public static DateOnly? Date(IReadOnlyDictionary<string, string?> values, params string[] keys)
    {
        var raw = Get(values, keys);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
            return DateOnly.FromDateTime(dateTime);
        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial is > 20000 and < 80000)
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        return null;
    }

    public static string RecipeKey(IReadOnlyDictionary<string, string?> values)
    {
        var recipeId = Get(values, "ItemCode", "Item Code", "RecipeID", "Recipe Code", "RecipeCode");
        if (!string.IsNullOrWhiteSpace(recipeId)) return recipeId;
        return $"__NEW__:{Get(values, "Title", "Name") ?? string.Empty}";
    }

    private static string Normalize(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
}
