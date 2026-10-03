using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class StockCountMath
{
    public static readonly (JustificationCategory Key, string Label)[] Categories =
    [
        (JustificationCategory.BREAKAGE, "Breakage"),
        (JustificationCategory.SPILLAGE, "Spillage"),
        (JustificationCategory.UNRECORDED_CONSUMPTION, "Unrecorded Consumption"),
        (JustificationCategory.UNRECORDED_TRANSFER, "Unrecorded Transfer"),
        (JustificationCategory.COMPLIMENTARY_GUEST_RECOVERY, "Complimentary / Guest Recovery"),
        (JustificationCategory.INCORRECT_PREVIOUS_COUNT, "Incorrect Previous Count"),
        (JustificationCategory.POS_RECIPE_MAPPING_ISSUE, "POS / Recipe Mapping Issue"),
        (JustificationCategory.UOM_PACK_CONVERSION_ISSUE, "UOM / Pack Conversion Issue"),
        (JustificationCategory.EXPIRED_OR_SPOILED, "Expired / Spoiled"),
        (JustificationCategory.THEFT_SUSPECTED_LOSS, "Suspected Loss"),
        (JustificationCategory.OTHER, "Other"),
    ];

    public static string Label(JustificationCategory category) =>
        Categories.First(item => item.Key == category).Label;

    public static decimal RoundQty(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    public static decimal Variance(decimal physicalQty, decimal systemQty) => RoundQty(physicalQty - systemQty);

    public static StockCountLineStatus Classify(decimal varianceQty) =>
        varianceQty < 0 ? StockCountLineStatus.SHORTAGE : varianceQty > 0 ? StockCountLineStatus.SURPLUS : StockCountLineStatus.MATCHED;

    public static decimal ShortageQty(decimal varianceQty) => varianceQty < 0 ? RoundQty(-varianceQty) : 0;

    public static decimal? ShortageValue(decimal varianceQty, decimal? unitCost) =>
        varianceQty < 0 && unitCost is not null ? RoundQty(ShortageQty(varianceQty) * unitCost.Value) : varianceQty > 0 && unitCost is not null ? RoundQty(varianceQty * unitCost.Value) : null;

    public static decimal? Percent(decimal varianceQty, decimal systemQty) =>
        systemQty == 0 ? null : RoundQty(varianceQty / systemQty * 100m);

    public static decimal ConvertToBase(decimal quantity, string fromUom, string baseUom, IReadOnlyList<MaterialUomConversion> conversions, Material material)
    {
        var converted = RecipeUom.Convert(quantity, fromUom, baseUom, RecipeUom.EffectiveConversions(material, conversions))
            ?? throw new RecipeManagementException("UOM_CONVERSION_MISSING", $"No conversion from {fromUom} to {baseUom}.");
        return RoundQty(converted);
    }
}
