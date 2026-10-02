using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class RecipeReadinessTests
{
    [Fact]
    public void Missing_price_blocks_readiness_but_is_not_priced_as_zero()
    {
        var rice = Unpriced("1006930", "Basmati Rice");
        var line = Line(rice, 150, "G");
        var readiness = RecipeReadinessEvaluator.Evaluate(
            [line],
            new Dictionary<Guid, Material> { [rice.Id] = rice },
            EmptyConversions(),
            new Dictionary<Guid, MaterialChangeRequest>());
        Assert.False(readiness.ReadyForApproval);
        Assert.Equal("NOT_READY", readiness.Status);
        Assert.Contains(readiness.Issues, item => item.Code == "PRICE_MISSING" && item.Message.Contains("Price Missing"));
        Assert.Equal("PRICE_MISSING", MaterialCosting.PriceStatus(rice, null));
        Assert.Equal("Price Missing", MaterialCosting.PriceStatusLabel("PRICE_MISSING"));
        Assert.Null(MaterialCosting.EffectiveUnitCost(rice));
        Assert.Null(RecipeCosting.LineCost(0.150m, MaterialCosting.EffectiveUnitCost(rice)));
    }

    [Fact]
    public void Approved_price_and_uom_conversion_make_recipe_ready()
    {
        var chicken = Priced("1006929", "Chicken Breast", 24m);
        var line = Line(chicken, 200, "G");
        var readiness = RecipeReadinessEvaluator.Evaluate(
            [line],
            new Dictionary<Guid, Material> { [chicken.Id] = chicken },
            EmptyConversions(),
            new Dictionary<Guid, MaterialChangeRequest>());
        Assert.True(readiness.ReadyForApproval);
        Assert.Equal("READY_FOR_APPROVAL", readiness.Status);
        Assert.Equal("PRICE_APPROVED", MaterialCosting.PriceStatus(chicken, null));
        var consumption = RecipeUom.Convert(200, "G", "KG", []);
        Assert.Equal(0.200m, consumption);
        Assert.Equal(4.80m, RecipeCosting.LineCost(consumption!.Value, MaterialCosting.EffectiveUnitCost(chicken)));
    }

    [Fact]
    public void Pending_material_price_is_informational_and_not_authoritative()
    {
        var rice = Unpriced("1006930", "Basmati Rice");
        var pending = new MaterialChangeRequest
        {
            MaterialCode = rice.MaterialCode,
            ProposedJson = MaterialNormalized.Serialize(new MaterialNormalizedRecord(
                rice.MaterialCode, rice.Description, rice.Description, null, null, null, rice.BaseUom, null, null, null, null, "V",
                null, 24m, "AED", 24m, MaterialAcquisitionSource.MANUAL, "RECIPE", null)),
            Status = MaterialGovernanceStatus.PENDING_APPROVAL,
        };
        var readiness = RecipeReadinessEvaluator.Evaluate(
            [Line(rice, 150, "G")],
            new Dictionary<Guid, Material> { [rice.Id] = rice },
            EmptyConversions(),
            new Dictionary<Guid, MaterialChangeRequest> { [rice.Id] = pending });
        Assert.False(readiness.ReadyForApproval);
        Assert.Contains(readiness.Issues, item => item.Code == "PRICE_PENDING_APPROVAL");
        Assert.Equal("PRICE_PENDING_APPROVAL", MaterialCosting.PriceStatus(rice, pending));
        Assert.Equal(24m, MaterialCosting.ProposedUnitPrice(pending));
        Assert.Null(MaterialCosting.EffectiveUnitCost(rice));
    }

    [Fact]
    public void Approved_price_stays_operational_while_a_replacement_is_pending()
    {
        var rice = Priced("1006930", "Basmati Rice", 8m);
        var pending = new MaterialChangeRequest
        {
            MaterialCode = rice.MaterialCode,
            ProposedJson = MaterialNormalized.Serialize(new MaterialNormalizedRecord(
                rice.MaterialCode, rice.Description, rice.Description, null, null, null, rice.BaseUom, null, null, null, null, "S",
                8.5m, null, "AED", 8.5m, MaterialAcquisitionSource.MANUAL, "RECIPE", null)),
            Status = MaterialGovernanceStatus.PENDING_APPROVAL,
            ProposedUnitPrice = 8.5m,
            CurrentUnitPrice = 8m,
        };
        var readiness = RecipeReadinessEvaluator.Evaluate(
            [Line(rice, 150, "G")],
            new Dictionary<Guid, Material> { [rice.Id] = rice },
            EmptyConversions(),
            new Dictionary<Guid, MaterialChangeRequest> { [rice.Id] = pending });
        Assert.True(readiness.ReadyForApproval);
        Assert.DoesNotContain(readiness.Issues, item => item.Code == "PRICE_PENDING_APPROVAL");
        Assert.Equal("PRICE_PENDING_APPROVAL", MaterialCosting.PriceStatus(rice, pending));
        Assert.Equal(8m, MaterialCosting.EffectiveUnitCost(rice));
        Assert.Equal(8.5m, MaterialCosting.ProposedUnitPrice(pending));
        Assert.Equal(1.20m, RecipeCosting.LineCost(0.150m, MaterialCosting.EffectiveUnitCost(rice)));
    }

    [Fact]
    public void Rejected_price_is_a_correctable_readiness_issue()
    {
        var vodka = new Material
        {
            Id = Guid.NewGuid(), MaterialCode = "1007001", Description = "Vodka 750ml",
            NormalizedDescription = "VODKA 750ML", BaseUom = "BOT", Status = StatusKind.ACTIVE,
            GovernanceStatus = MaterialGovernanceStatus.REJECTED,
        };
        Assert.Equal("PRICE_REJECTED", MaterialCosting.PriceStatus(vodka, null));
        Assert.Equal("Rejected", MaterialCosting.PriceStatusLabel("PRICE_REJECTED"));
        var readiness = RecipeReadinessEvaluator.Evaluate(
            [Line(vodka, 1, "BOT")],
            new Dictionary<Guid, Material> { [vodka.Id] = vodka },
            EmptyConversions(),
            new Dictionary<Guid, MaterialChangeRequest>());
        Assert.Contains(readiness.Issues, item => item.Code == "PRICE_REJECTED");
        Assert.False(readiness.ReadyForApproval);
    }

    [Fact]
    public void Material_specific_pack_conversion_drives_inventory_consumption()
    {
        var vodka = Priced("1005001", "Premium Vodka 750 ML", 75m);
        vodka.BaseUom = "EA";
        var conversions = new Dictionary<Guid, IReadOnlyList<MaterialUomConversion>>
        {
            [vodka.Id] = [new MaterialUomConversion { FromUom = "EA", ToUom = "ML", Numerator = 750, Denominator = 1, IsActive = true }],
        };
        var consumption = RecipeUom.Convert(30, "ML", "EA", conversions[vodka.Id]);
        Assert.Equal(0.040m, consumption);
        Assert.Equal(3.00m, RecipeCosting.LineCost(consumption!.Value, MaterialCosting.EffectiveUnitCost(vodka)));
    }

    [Fact]
    public void Material_conv_columns_drive_recipe_uom_and_qty_conversion()
    {
        var whiskey = Priced("WHISKEY", "Whiskey", 200m);
        whiskey.BaseUom = "EA";
        whiskey.ConvFactor = 1m;
        whiskey.ConvUnit = "ML";
        whiskey.ConvValue = 750m;
        var alt = RecipeUom.Alternate(whiskey);
        Assert.NotNull(alt);
        Assert.Equal(1m, alt.Value.Factor);
        Assert.Equal("ML", alt.Value.Unit);
        Assert.Equal(750m, alt.Value.Value);
        var conversions = new[]
        {
            new MaterialUomConversion { FromUom = whiskey.BaseUom, ToUom = alt.Value.Unit, Numerator = alt.Value.Value, Denominator = alt.Value.Factor, IsActive = true },
        };
        Assert.Equal(1m, RecipeUom.Convert(750m, "ML", "EA", conversions));
        Assert.Equal(0.040m, RecipeUom.Convert(30m, "ML", "EA", conversions));
        Assert.Equal(8.00m, RecipeCosting.LineCost(0.040m, MaterialCosting.EffectiveUnitCost(whiskey)));
        Assert.Equal("1 EA = 750 ML", RecipeUom.Formula(whiskey.BaseUom, whiskey.ConvFactor, whiskey.ConvUnit, whiskey.ConvValue));
    }

    [Fact]
    public void Conv_columns_complete_readiness_without_conversion_table_rows()
    {
        var whiskey = Priced("WHISKEY", "Whiskey", 200m);
        whiskey.BaseUom = "EA";
        whiskey.ConvFactor = 1m;
        whiskey.ConvUnit = "ML";
        whiskey.ConvValue = 750m;
        var readiness = RecipeReadinessEvaluator.Evaluate(
            [Line(whiskey, 750, "ML")],
            new Dictionary<Guid, Material> { [whiskey.Id] = whiskey },
            EmptyConversions(),
            new Dictionary<Guid, MaterialChangeRequest>());
        Assert.True(readiness.ReadyForApproval);
        Assert.Equal(1m, RecipeUom.Convert(750m, "ML", "EA", RecipeUom.EffectiveConversions(whiskey)));
    }

    private static Material Unpriced(string code, string name) => new()
    {
        Id = Guid.NewGuid(), MaterialCode = code, Description = name, NormalizedDescription = name.ToUpperInvariant(),
        BaseUom = "KG", Status = StatusKind.ACTIVE, GovernanceStatus = MaterialGovernanceStatus.DRAFT,
    };

    private static Material Priced(string code, string name, decimal price) => new()
    {
        Id = Guid.NewGuid(), MaterialCode = code, Description = name, NormalizedDescription = name.ToUpperInvariant(),
        BaseUom = "KG", Status = StatusKind.ACTIVE, GovernanceStatus = MaterialGovernanceStatus.ACTIVE,
        PriceControl = "S", StandardPrice = price, Currency = "AED",
    };

    private static RecipeIngredient Line(Material material, decimal quantity, string uom) => new()
    {
        MaterialId = material.Id, ErpMaterialId = material.MaterialCode, MaterialDescription = material.Description,
        Quantity = quantity, Uom = uom, Sequence = 10,
    };

    private static Dictionary<Guid, IReadOnlyList<MaterialUomConversion>> EmptyConversions() => [];
}
