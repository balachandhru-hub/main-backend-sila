using SilaMe.Api.DTOs;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class MaterialNormalizedTests
{
    [Fact]
    public void Merge_keeps_one_material_and_all_valuation_areas()
    {
        var merged = MaterialNormalized.MergeByProduct(
        [
            new MaterialNormalizedRecord("1006929", "Chicken", "Chicken", "ROH", "FOOD", null, "KG", null, "1050", "1050", "3000", "S", 40m, null, "AED", null, SilaMe.Api.Models.MaterialAcquisitionSource.ERP, "SAP_S4HANA", null),
            new MaterialNormalizedRecord("1006929", "Chicken", "Chicken", "ROH", "FOOD", null, "KG", null, "1050", "1060", "3000", "V", null, 42m, "AED", null, SilaMe.Api.Models.MaterialAcquisitionSource.ERP, "SAP_S4HANA", null),
        ]);
        var item = Assert.Single(merged);
        Assert.Equal("1006929", item.MaterialCode);
        Assert.Equal(2, item.Valuations!.Count);
        Assert.Equal(40m, item.UnitCost);
        Assert.Equal("S", item.PriceControl);
    }

    [Fact]
    public void Excel_reads_alternate_uom_conversion_columns()
    {
        var record = MaterialNormalized.FromExcel(new Dictionary<string, string?>
        {
            ["MaterialID"] = "WHISKEY",
            ["Description"] = "Whiskey",
            ["BaseUOM"] = "EA",
            ["ConvFactor"] = "1",
            ["ConvUnit"] = "ML",
            ["ConvValue"] = "750",
        });
        Assert.Equal(1m, record.ConvFactor);
        Assert.Equal("ML", record.ConvUnit);
        Assert.Equal(750m, record.ConvValue);
        Assert.Equal("1 EA = 750 ML", RecipeUom.Formula(record.BaseUom, record.ConvFactor, record.ConvUnit, record.ConvValue));
    }

    [Fact]
    public void Price_control_does_not_arbitrarily_pick_moving_average()
    {
        Assert.Equal(10m, MaterialCosting.FromPriceControl("S", 10m, 99m, 1m));
        Assert.Equal(99m, MaterialCosting.FromPriceControl("V", 10m, 99m, 1m));
        Assert.Equal(1m, MaterialCosting.FromPriceControl(null, 10m, 99m, 1m));
    }
}
