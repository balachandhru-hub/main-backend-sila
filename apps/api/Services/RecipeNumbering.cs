using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class RecipeNumbering
{
    public const string RecipePrefix = "RI";

    public static string RecipeId(int sequence) => $"{RecipePrefix}{sequence.ToString().PadLeft(8, '0')}";

    public static string IngredientId(string recipeId, int lineNumber) => $"{recipeId}I{lineNumber}";

    public static int MaxRecipeSequence(IEnumerable<string> codes)
    {
        var max = 0;
        foreach (var code in codes)
        {
            if (code.Length == 10 && code.StartsWith(RecipePrefix, StringComparison.OrdinalIgnoreCase)
                && int.TryParse(code[2..], out var number))
                max = Math.Max(max, number);
        }
        return max;
    }
}
