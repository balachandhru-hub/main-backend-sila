using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class RecipeNumberingTests
{
    [Fact]
    public void Recipe_ids_start_at_RI00000001()
    {
        Assert.Equal("RI00000001", RecipeNumbering.RecipeId(1));
        Assert.Equal("RI00000002", RecipeNumbering.RecipeId(2));
        Assert.Equal("RI00000100", RecipeNumbering.RecipeId(100));
    }

    [Fact]
    public void Ingredient_ids_append_I_and_line_number_to_recipe_id()
    {
        Assert.Equal("RI00000001I1", RecipeNumbering.IngredientId("RI00000001", 1));
        Assert.Equal("RI00000001I2", RecipeNumbering.IngredientId("RI00000001", 2));
    }

    [Fact]
    public void Max_sequence_reads_RI_plus_eight_digits()
    {
        Assert.Equal(0, RecipeNumbering.MaxRecipeSequence([]));
        Assert.Equal(3, RecipeNumbering.MaxRecipeSequence(["RI00000001", "RI00000003", "MENU-1"]));
    }
}
