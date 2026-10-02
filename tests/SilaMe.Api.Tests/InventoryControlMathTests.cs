using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class InventoryControlMathTests
{
    [Fact]
    public void Stockout_is_zero_available_at_a_stocking_location()
    {
        Assert.Equal(InventoryControlMath.Out, InventoryControlMath.Classify(0, 5, 20));
        Assert.Equal(InventoryControlMath.Low, InventoryControlMath.Classify(3, 5, 20));
        Assert.Equal(InventoryControlMath.Healthy, InventoryControlMath.Classify(8, 5, 20));
        Assert.Equal(InventoryControlMath.Excess, InventoryControlMath.Classify(25, 5, 20));
        Assert.Equal(InventoryControlMath.Healthy, InventoryControlMath.Classify(3, null, null));
    }

    [Fact]
    public void Replenishment_uses_par_minus_available_when_reorder_is_breached()
    {
        Assert.Equal(7m, InventoryControlMath.RecommendedQty(3, 5, 10));
        Assert.Null(InventoryControlMath.RecommendedQty(8, 5, 10));
        Assert.Null(InventoryControlMath.RecommendedQty(3, 5, null));
        Assert.Null(InventoryControlMath.RecommendedQty(12, 5, 10));
        Assert.Equal(18m, InventoryControlMath.TransferableQty(24, 6, 4));
    }

    [Fact]
    public void Compact_money_keeps_currency()
    {
        Assert.Equal("AED 989.3M", InventoryControlMath.CompactMoney(989339800m, "AED"));
    }
}
