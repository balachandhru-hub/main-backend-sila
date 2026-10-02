using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class RecipePosSalesTests
{
    [Fact]
    public void Outlet_hyphens_match_location_master_codes()
    {
        Assert.Equal("FL15", RecipePosSales.NormalizeOutlet("FL-15"));
        Assert.Equal("FL12", RecipePosSales.NormalizeOutlet("FL12"));
        Assert.Equal("FL13", RecipePosSales.NormalizeOutlet(" fl 13 "));
    }

    [Fact]
    public void Sales_row_requires_the_template_columns()
    {
        var valid = RecipePosSales.ParseRow(2, new Dictionary<string, string?>
        {
            ["BusinessDate"] = "2026-09-27",
            ["TransactionID"] = "TXN100234",
            ["LineID"] = "10",
            ["POSCode"] = "CKTL001",
            ["Qty"] = "3",
            ["UOM"] = "EA",
            ["OutletID"] = "FL12",
            ["Currency"] = "AED",
        });
        Assert.True(valid.IsValid);
        Assert.Equal("CKTL001", valid.PosCode);
        Assert.Equal(3m, valid.Qty);
        Assert.Equal(10, valid.LineId);

        var invalid = RecipePosSales.ParseRow(3, new Dictionary<string, string?> { ["POSCode"] = "CKTL001" });
        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, item => item.Contains("BusinessDate"));
        Assert.Contains(invalid.Errors, item => item.Contains("TransactionID"));
        Assert.Contains(invalid.Errors, item => item.Contains("LineID"));
        Assert.Contains(invalid.Errors, item => item.Contains("OutletID"));
        Assert.Contains(invalid.Errors, item => item.Contains("Currency"));
    }

    [Fact]
    public void Tracker_maps_outlet_id_to_location_master_outlet_store_or_venue()
    {
        var org = Guid.NewGuid();
        var property = Loc(org, "PROP1", "Marina Property", InventoryLocationType.PROPERTY);
        var outlet = Loc(org, "FL15", "Floor 15 Lounge", InventoryLocationType.OUTLET);
        var store = Loc(org, "FL13", "Burger Store", InventoryLocationType.STORE);
        var venue = Loc(org, "FL12", "Pool Venue", InventoryLocationType.VENUE);
        var locations = new[] { property, outlet, store, venue };

        Assert.Equal(outlet.Id, RecipePosSales.FindStockingLocation(locations, "FL-15", "P1000", "0001")!.Id);
        Assert.Equal(store.Id, RecipePosSales.FindStockingLocation(locations, "FL13")!.Id);
        Assert.Equal(venue.Id, RecipePosSales.FindStockingLocation(locations, "FL12")!.Id);
        Assert.Null(RecipePosSales.FindStockingLocation(locations, "PROP1"));
        Assert.Equal("Outlet", RecipePosSales.LocationKindLabel(InventoryLocationType.OUTLET));
        Assert.Equal("Store", RecipePosSales.LocationKindLabel(InventoryLocationType.STORE));
        Assert.Equal("Venue", RecipePosSales.LocationKindLabel(InventoryLocationType.VENUE));

        var transaction = new RecipeConsumptionTransaction
        {
            Id = Guid.NewGuid(), OrganizationId = org, PosSourceId = Guid.NewGuid(), SourceSystem = "EXCEL",
            SourceTransactionId = "TXN100236", PosOutletCode = "FL-15", PosItemCode = "COKE001",
            RawReference = RecipePosSales.PackRaw(null, outlet.Id, "EA", "AED", "FL-15"),
        };
        Assert.Equal(outlet.Id, RecipePosSales.LocationForTransaction(locations, transaction)!.Id);
    }

    private static InventoryLocation Loc(Guid org, string code, string name, InventoryLocationType type) => new()
    {
        Id = Guid.NewGuid(), OrganizationId = org, LocationCode = code, LocationName = name, LocationType = type, Status = StatusKind.ACTIVE,
    };
}
