using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class RecipeCostingTests
{
    [Fact]
    public void Explosion_divides_by_serving_qty_for_batch_recipes()
    {
        var rice = new RecipeIngredient { Quantity = 5m, WastagePercent = 0, YieldPercent = 100, Uom = "KG", Sequence = 1 };
        Assert.Equal(1m, RecipeCosting.ConsumedQuantity(rice, 4, 20));
        var gin = new RecipeIngredient { Quantity = 30m, WastagePercent = 0, YieldPercent = 100, Uom = "ML", Sequence = 1 };
        Assert.Equal(90m, RecipeCosting.ConsumedQuantity(gin, 3, 1));
    }

    [Fact]
    public void Cost_per_portion_divides_total_by_serving_qty()
    {
        var ingredients = new[]
        {
            new RecipeIngredient { Quantity = 0.20m, WastagePercent = 0, YieldPercent = 100, UnitCost = 40m, Uom = "KG" },
            new RecipeIngredient { Quantity = 0.15m, WastagePercent = 0, YieldPercent = 100, UnitCost = 10m, Uom = "KG" },
        };
        var total = RecipeCosting.RecipeTotalCost(ingredients);
        Assert.Equal(9.50m, total);
        Assert.Equal(9.50m, RecipeCosting.CostPerPortion(total, 1));
        Assert.Equal(4.75m, RecipeCosting.CostPerPortion(total, 2));
        var profit = RecipeUom.Profitability(20m, 4m, 50m);
        Assert.Equal(5m, profit.CostPerServing);
        Assert.Equal(10m, profit.CostPercent);
    }

    [Fact]
    public void Food_cost_percentages_use_ingredient_total_and_menu_price()
    {
        Assert.Equal(40m, RecipeCosting.PercentageOfTotal(4m, 10m));
        Assert.Equal(25m, RecipeCosting.PosItemCostPercentage(25m, 100m));
        Assert.Null(RecipeCosting.PosItemCostPercentage(25m, 0m));
    }

    [Fact]
    public void Wastage_and_yield_change_effective_quantity()
    {
        Assert.Equal(0.22m, RecipeCosting.EffectiveQuantity(0.20m, 10, 100));
        Assert.Equal(0.40m, RecipeCosting.EffectiveQuantity(0.20m, 0, 50));
    }

    [Fact]
    public void Line_cost_is_quantity_times_material_price_and_null_when_price_missing()
    {
        Assert.Equal(8m, RecipeCosting.LineCost(0.2m, 40m));
        Assert.Null(RecipeCosting.LineCost(0.2m, null));
    }

    [Fact]
    public void Total_serving_uses_primary_ingredient_uom_when_mixed()
    {
        var serving = RecipeCosting.TotalServing([
            (0.200m, "KG", 10),
            (0.150m, "KG", 20),
            (0.020m, "L", 30),
            (0.010m, "KG", 40),
        ]);
        Assert.Equal("KG", serving.Uom);
        Assert.Equal(0.360m, serving.Quantity);
    }

    [Fact]
    public void Mixed_uom_percentage_falls_back_to_cost_share()
    {
        var all = new List<(decimal Quantity, string Uom, decimal? Cost, int Sequence)>
        {
            (0.200m, "KG", 4m, 10),
            (0.150m, "KG", 1.2m, 20),
            (0.020m, "L", 0.4m, 30),
        };
        Assert.Equal(71.4286m, RecipeCosting.QuantityOrCostShare(0.200m, "KG", 4m, 10, all));
    }

    [Fact]
    public void Cost_percent_uses_ingredient_cost_not_quantity()
    {
        var all = new List<(decimal Quantity, string Uom, decimal? Cost, int Sequence)>
        {
            (1m, "KG", 6m, 10),
            (1m, "KG", 2m, 20),
            (1m, "KG", 2m, 30),
        };
        Assert.Equal(60m, RecipeCosting.QuantityOrCostShare(1m, "KG", 6m, 10, all));
        Assert.Equal(20m, RecipeCosting.QuantityOrCostShare(1m, "KG", 2m, 20, all));
    }

    [Fact]
    public void Missing_unit_price_is_not_treated_as_zero()
    {
        var draft = new Material { MaterialCode = "1006930", Description = "Basmati Rice", NormalizedDescription = "BASMATI RICE", BaseUom = "KG", GovernanceStatus = MaterialGovernanceStatus.DRAFT };
        var priced = new Material { MaterialCode = "1006929", Description = "Chicken Breast", NormalizedDescription = "CHICKEN BREAST", BaseUom = "KG", GovernanceStatus = MaterialGovernanceStatus.ACTIVE, PriceControl = "S", StandardPrice = 18.50m };
        var zero = new Material { MaterialCode = "1006999", Description = "Water", NormalizedDescription = "WATER", BaseUom = "L", GovernanceStatus = MaterialGovernanceStatus.ACTIVE, PriceControl = "V", MovingAveragePrice = 0m };
        Assert.False(MaterialCosting.HasValidUnitPrice(draft));
        Assert.Null(RecipeCosting.LineCost(1m, MaterialCosting.AuthoritativeUnitPrice(draft)));
        Assert.True(MaterialCosting.HasValidUnitPrice(priced));
        Assert.Equal(18.50m, MaterialCosting.AuthoritativeUnitPrice(priced));
        Assert.True(MaterialCosting.HasValidUnitPrice(zero));
        Assert.Equal(0m, MaterialCosting.AuthoritativeUnitPrice(zero));
    }
}
