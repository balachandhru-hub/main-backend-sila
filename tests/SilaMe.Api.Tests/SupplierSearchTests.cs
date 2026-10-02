using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class SupplierSearchTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("ABC")]
    [InlineData("Abc")]
    [InlineData("abc food")]
    [InlineData("FOOD")]
    [InlineData("food trading")]
    [InlineData("ABC FOOD TRADING")]
    [InlineData("ABC Food Trading L.L.C.")]
    [InlineData("1000234")]
    public void Friendly_queries_rank_abc_food_trading_first(string query)
    {
        var match = CreateSupplier("1000234", "ABC FOOD TRADING LLC", "ABC Food Trading L.L.C.");
        var other = CreateSupplier("1000999", "GLOBAL FOOD SUPPLIES LLC");
        var third = CreateSupplier("1000888", "FRESH FOOD INTERNATIONAL LLC");
        var ranked = SupplierSearch.Rank([other, third, match], query);
        Assert.Equal("1000234", ranked[0].SupplierCode);
        Assert.Contains(ranked, item => item.SupplierCode == "1000234");
    }

    [Fact]
    public void Exact_supplier_id_label_ranks_code_match_first()
    {
        var match = CreateSupplier("1003430", "Test SBN");
        var other = CreateSupplier("1000999", "GLOBAL FOOD SUPPLIES LLC");
        var rankedBare = SupplierSearch.Rank([other, match], "ID: 1003430");
        Assert.Equal("1003430", rankedBare[0].SupplierCode);
        Assert.Equal("Test SBN", rankedBare[0].Name);
    }

    [Fact]
    public void Normalize_folds_case_punctuation_and_spaces()
    {
        Assert.Equal("ABC FOOD TRADING LLC", SupplierSearch.Normalize("  ABC   Food Trading  L.L.C. "));
        Assert.Equal("ABC FOOD TRADING LLC", SupplierSearch.Normalize("ABC Food Trading L.L.C."));
    }

    private static Supplier CreateSupplier(string code, string name, string? alias = null)
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            SupplierCode = code,
            Name = name,
            NormalizedName = SupplierSearch.Normalize(name),
            LegalName = name,
            SearchName = name,
            Status = StatusKind.ACTIVE,
        };
        if (alias is not null)
        {
            supplier.Aliases.Add(new SupplierAlias
            {
                Id = Guid.NewGuid(),
                OrganizationId = supplier.OrganizationId,
                SupplierId = supplier.Id,
                Alias = alias,
                NormalizedAlias = SupplierSearch.Normalize(alias),
                IsConfirmed = true,
            });
        }
        return supplier;
    }
}
