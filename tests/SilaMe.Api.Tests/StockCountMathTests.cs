using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public class StockCountMathTests
{
    [Fact]
    public void Shortage_is_physical_minus_system()
    {
        var variance = StockCountMath.Variance(7m, 8.4m);
        Assert.Equal(-1.4m, variance);
        Assert.Equal(StockCountLineStatus.SHORTAGE, StockCountMath.Classify(variance));
        Assert.Equal(1.4m, StockCountMath.ShortageQty(variance));
    }

    [Fact]
    public void Surplus_is_positive_and_matched_is_zero()
    {
        Assert.Equal(0.6m, StockCountMath.Variance(9m, 8.4m));
        Assert.Equal(StockCountLineStatus.SURPLUS, StockCountMath.Classify(0.6m));
        Assert.Equal(StockCountLineStatus.MATCHED, StockCountMath.Classify(0));
    }

    [Fact]
    public void Partial_pack_uses_material_conversion()
    {
        var material = new Material
        {
            Id = Guid.NewGuid(), MaterialCode = "1000000", Description = "Bombay Sapphire 750ml", NormalizedDescription = "BOMBAY",
            BaseUom = "EA", ConvUnit = "ML", ConvValue = 750, ConvFactor = 1,
        };
        var full = StockCountMath.ConvertToBase(7, "EA", "EA", [], material);
        var open = StockCountMath.ConvertToBase(300, "ML", "EA", [], material);
        var physical = StockCountMath.RoundQty(full + open);
        Assert.Equal(7.4m, physical);
        Assert.Equal(-1.0m, StockCountMath.Variance(physical, 8.4m));
    }

    [Fact]
    public void Sap_quantity_is_absolute_and_movement_follows_sign()
    {
        var shortage = RecipeSapUpdateStock.BuildRequestJson(new DateOnly(2026, 10, 31),
        [
            new RecipeSapStockItem(new DateOnly(2026, 10, 31), "P", "S", "1000000", RecipeSapUpdateStock.MovementType(-1.4m), 1.4m, "EA", []),
        ]);
        Assert.Contains("\"GoodsMovementCode\":\"03\"", shortage);
        Assert.Contains("\"GoodsMovementType\":\"Z02\"", shortage);
        Assert.Contains("\"QuantityInEntryUnit\":\"1.400\"", shortage);
        Assert.DoesNotContain("-1.400", shortage);
        Assert.Equal("Z01", RecipeSapUpdateStock.MovementType(0.6m));
    }
}
