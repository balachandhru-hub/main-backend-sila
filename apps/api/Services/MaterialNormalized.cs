using System.Globalization;
using System.Text.Json;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class MaterialNormalized
{
    public static readonly string[] TemplateColumns =
    [
        "Material ID", "Material Name", "Description", "Material Type", "Material Group", "Category",
        "Base UOM", "Alternate UOM", "ConvFactor", "ConvUnit", "ConvValue", "Valuation Area", "Valuation Class", "Price Control",
        "Standard Price", "Moving Average Price", "Currency", "Unit Cost", "Active",
        "InventoryItem", "InventoryType", "BatchManaged", "ExpiryManaged", "ShelfLifeDays", "SerialManaged"
    ];

    public static readonly string[] MaterialsSheetColumns =
    [
        "MaterialID", "Description", "MaterialType", "MaterialGroup", "Family", "Category", "BaseUOM",
        "ConvFactor", "ConvUnit", "ConvValue", "Active",
        "InventoryItem", "InventoryType", "BatchManaged", "ExpiryManaged", "ShelfLifeDays", "SerialManaged"
    ];

    public static readonly string[] ValuationSheetColumns =
    [
        "MaterialID", "ValuationArea", "Plant", "PriceControl", "StandardPrice", "MovingAveragePrice", "EffectivePrice", "PriceUOM", "Currency"
    ];

    public static readonly string[] ConversionSheetColumns =
    [
        "MaterialID", "FromUOM", "ToUOM", "Numerator", "Denominator", "ConvFactor", "ConvUnit", "ConvValue", "PackSize", "PackUOM", "Active"
    ];

    public static readonly string[] ExportColumns =
    [
        ..TemplateColumns, "Company Code", "Source", "Approval Status", "Active Status"
    ];

    public static MaterialNormalizedRecord FromExcel(IReadOnlyDictionary<string, string?> values)
    {
        var active = Get(values, "ACTIVE", "STATUS");
        return new MaterialNormalizedRecord(
            Get(values, "MATERIAL_ID", "MATERIAL_CODE", "PRODUCT") ?? string.Empty,
            Get(values, "MATERIAL_NAME", "NAME", "PRODUCT_DESCRIPTION") ?? Get(values, "DESCRIPTION") ?? string.Empty,
            Get(values, "DESCRIPTION"),
            Get(values, "MATERIAL_TYPE", "PRODUCT_TYPE"),
            Get(values, "MATERIAL_GROUP", "PRODUCT_GROUP", "FAMILY"),
            Get(values, "CATEGORY") ?? Get(values, "FAMILY"),
            Get(values, "BASE_UOM", "BASE_UNIT", "UOM", "PRICE_UOM") ?? string.Empty,
            Get(values, "ALTERNATE_UOM"),
            Get(values, "COMPANY_CODE"),
            Get(values, "VALUATION_AREA", "PLANT"),
            Get(values, "VALUATION_CLASS"),
            Get(values, "PRICE_CONTROL"),
            ParseDecimal(Get(values, "STANDARD_PRICE")),
            ParseDecimal(Get(values, "MOVING_AVERAGE_PRICE")),
            Get(values, "CURRENCY"),
            ParseDecimal(Get(values, "UNIT_COST", "EFFECTIVE_PRICE")),
            MaterialAcquisitionSource.EXCEL,
            "EXCEL",
            null,
            string.Equals(active, "INACTIVE", StringComparison.OrdinalIgnoreCase) || string.Equals(active, "N", StringComparison.OrdinalIgnoreCase)
                ? false
                : true,
            null,
            ParseDecimal(Get(values, "CONVFACTOR")),
            Get(values, "CONVUNIT"),
            ParseDecimal(Get(values, "CONVVALUE")),
            ParseBool(Get(values, "INVENTORYITEM")),
            Get(values, "INVENTORYTYPE") ?? "NON_STOCK",
            ParseBool(Get(values, "BATCHMANAGED")),
            ParseBool(Get(values, "EXPIRYMANAGED")),
            int.TryParse(Get(values, "SHELFLIFEDAYS"), out var days) ? days : null,
            ParseBool(Get(values, "SERIALMANAGED")));
    }

    public static Dictionary<string, string?> ToExcel(MaterialNormalizedRecord item, string? governance = null, string? activeStatus = null) => new()
    {
        ["Material ID"] = item.MaterialCode,
        ["Material Name"] = item.Name,
        ["Description"] = item.Description,
        ["Material Type"] = item.MaterialType,
        ["Material Group"] = item.MaterialGroup,
        ["Category"] = item.Category,
        ["Base UOM"] = item.BaseUom,
        ["Alternate UOM"] = item.AlternateUom,
        ["ConvFactor"] = Format(item.ConvFactor),
        ["ConvUnit"] = item.ConvUnit,
        ["ConvValue"] = Format(item.ConvValue),
        ["Valuation Area"] = item.ValuationArea,
        ["Valuation Class"] = item.ValuationClass,
        ["Price Control"] = item.PriceControl,
        ["Standard Price"] = Format(item.StandardPrice),
        ["Moving Average Price"] = Format(item.MovingAveragePrice),
        ["Currency"] = item.Currency,
        ["Unit Cost"] = Format(item.UnitCost),
        ["Active"] = item.Active == false ? "INACTIVE" : "ACTIVE",
        ["InventoryItem"] = item.InventoryItem ? "TRUE" : "FALSE",
        ["InventoryType"] = item.InventoryType,
        ["BatchManaged"] = item.BatchManaged ? "TRUE" : "FALSE",
        ["ExpiryManaged"] = item.ExpiryManaged ? "TRUE" : "FALSE",
        ["ShelfLifeDays"] = item.ShelfLifeDays?.ToString(CultureInfo.InvariantCulture),
        ["SerialManaged"] = item.SerialManaged ? "TRUE" : "FALSE",
        ["Company Code"] = item.CompanyCode,
        ["Source"] = item.Source.ToString(),
        ["Approval Status"] = governance,
        ["Active Status"] = activeStatus,
    };

    public static IReadOnlyList<MaterialNormalizedRecord> MergeByProduct(IEnumerable<MaterialNormalizedRecord> records)
    {
        return records
            .Where(item => !string.IsNullOrWhiteSpace(item.MaterialCode))
            .GroupBy(item => item.MaterialCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var header = group.First();
                var valuations = group
                    .Where(item => !string.IsNullOrWhiteSpace(item.ValuationArea))
                    .GroupBy(item => item.ValuationArea!.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(item => item.Last())
                    .ToList();
                var primary = valuations.FirstOrDefault() ?? header;
                return header with
                {
                    Name = First(group.Select(item => item.Name)) ?? header.MaterialCode,
                    Description = First(group.Select(item => item.Description)) ?? header.Description,
                    MaterialType = First(group.Select(item => item.MaterialType)),
                    MaterialGroup = First(group.Select(item => item.MaterialGroup)),
                    Category = First(group.Select(item => item.Category)),
                    BaseUom = First(group.Select(item => item.BaseUom)) ?? header.BaseUom,
                    AlternateUom = First(group.Select(item => item.AlternateUom)),
                    ConvFactor = group.Select(item => item.ConvFactor).FirstOrDefault(item => item is not null),
                    ConvUnit = First(group.Select(item => item.ConvUnit)),
                    ConvValue = group.Select(item => item.ConvValue).FirstOrDefault(item => item is not null),
                    CompanyCode = primary.CompanyCode ?? header.CompanyCode,
                    ValuationArea = primary.ValuationArea,
                    ValuationClass = primary.ValuationClass,
                    PriceControl = primary.PriceControl,
                    StandardPrice = primary.StandardPrice,
                    MovingAveragePrice = primary.MovingAveragePrice,
                    Currency = primary.Currency ?? header.Currency,
                    UnitCost = MaterialCosting.FromPriceControl(primary.PriceControl, primary.StandardPrice, primary.MovingAveragePrice, primary.UnitCost ?? header.UnitCost),
                    SourceLastChangedAt = group.Select(item => item.SourceLastChangedAt).Where(item => item is not null).Max(),
                    Valuations = valuations.Select(item => ToValuationRecord(item)).ToList(),
                };
            })
            .ToList();
    }

    public static string Serialize(MaterialNormalizedRecord record) => JsonSerializer.Serialize(record);
    public static MaterialNormalizedRecord Deserialize(string json) =>
        JsonSerializer.Deserialize<MaterialNormalizedRecord>(json) ?? throw new RecipeManagementException("MATERIAL_CHANGE_INVALID", "The proposed material change could not be read.");

    public static bool SameOperational(MaterialNormalizedRecord left, Material existing, IReadOnlyList<MaterialValuation> valuations)
    {
        var current = FromEntity(existing, valuations);
        return string.Equals(left.Name, current.Name, StringComparison.Ordinal)
            && string.Equals(left.Description ?? "", current.Description ?? "", StringComparison.Ordinal)
            && string.Equals(left.MaterialType ?? "", current.MaterialType ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.MaterialGroup ?? "", current.MaterialGroup ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.BaseUom, current.BaseUom, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.ValuationArea ?? "", current.ValuationArea ?? "", StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.PriceControl ?? "", current.PriceControl ?? "", StringComparison.OrdinalIgnoreCase)
            && left.StandardPrice == current.StandardPrice
            && left.MovingAveragePrice == current.MovingAveragePrice
            && string.Equals(left.Currency ?? "", current.Currency ?? "", StringComparison.OrdinalIgnoreCase)
            && left.InventoryItem == current.InventoryItem
            && string.Equals(left.InventoryType, current.InventoryType, StringComparison.OrdinalIgnoreCase)
            && left.BatchManaged == current.BatchManaged
            && left.ExpiryManaged == current.ExpiryManaged
            && left.ShelfLifeDays == current.ShelfLifeDays
            && left.SerialManaged == current.SerialManaged;
    }

    public static bool HasAlternateConversion(MaterialNormalizedRecord record) =>
        !string.IsNullOrWhiteSpace(record.ConvUnit) && record.ConvValue is > 0;

    public static void ApplyConversion(Material material, MaterialNormalizedRecord record)
    {
        if (!HasAlternateConversion(record)) return;
        material.ConvFactor = record.ConvFactor is > 0 ? record.ConvFactor : 1m;
        material.ConvUnit = RecipeUom.Normalize(record.ConvUnit);
        material.ConvValue = record.ConvValue;
        if (material.ConvUnit is not null && string.IsNullOrWhiteSpace(material.AlternateUom))
            material.AlternateUom = material.ConvUnit;
        material.UpdatedAt = DateTime.UtcNow;
    }

    public static MaterialNormalizedRecord FromEntity(Material item, IEnumerable<MaterialValuation>? valuations = null) => new(
        item.MaterialCode,
        item.Description,
        item.Description,
        item.MaterialType,
        item.MaterialGroup,
        item.Category,
        item.BaseUom,
        item.AlternateUom,
        item.CompanyCode,
        item.ValuationArea,
        item.ValuationClass,
        item.PriceControl,
        item.StandardPrice,
        item.MovingAveragePrice,
        item.Currency,
        item.UnitCost,
        item.AcquisitionSource,
        item.SourceSystem,
        item.SourceLastChangedAt,
        item.Status == StatusKind.ACTIVE,
        CurrentValuations(valuations).Select(ToValuationRecord).ToList(),
        item.ConvFactor, item.ConvUnit, item.ConvValue,
        item.InventoryItem, item.InventoryType.ToString(), item.BatchManaged, item.ExpiryManaged, item.ShelfLifeDays, item.SerialManaged);

    public static IReadOnlyList<MaterialValuation> CurrentValuations(IEnumerable<MaterialValuation>? valuations) =>
        (valuations ?? []).Where(item => item.EffectiveTo is null).ToList();

    public static MaterialValuationRecord ToValuationRecord(MaterialValuation row) =>
        new(row.CompanyCode, row.ValuationArea, row.ValuationClass, row.PriceControl, row.StandardPrice, row.MovingAveragePrice,
            row.Currency, row.Plant, row.PriceUom, row.EffectiveFrom, row.EffectiveTo);

    public static MaterialValuationRecord ToValuationRecord(MaterialNormalizedRecord item) =>
        new(item.CompanyCode, item.ValuationArea ?? "DEFAULT", item.ValuationClass, item.PriceControl, item.StandardPrice,
            item.MovingAveragePrice, item.Currency);

    public static void Apply(Material material, MaterialNormalizedRecord record, bool operational)
    {
        material.Description = string.IsNullOrWhiteSpace(record.Description)
            ? (record.Name ?? string.Empty).Trim()
            : record.Description.Trim();
        material.NormalizedDescription = RecipeCosting.NormalizeKey(record.Name);
        material.MaterialType = Empty(record.MaterialType);
        material.MaterialGroup = Empty(record.MaterialGroup);
        material.Category = Empty(record.Category);
        material.BaseUom = string.IsNullOrWhiteSpace(record.BaseUom) ? material.BaseUom : record.BaseUom.Trim();
        material.AlternateUom = Empty(record.AlternateUom);
        if (HasAlternateConversion(record))
        {
            material.ConvFactor = record.ConvFactor is > 0 ? record.ConvFactor : 1m;
            material.ConvUnit = Empty(record.ConvUnit) is { } unit ? RecipeUom.Normalize(unit) : null;
            material.ConvValue = record.ConvValue is > 0 ? record.ConvValue : null;
            if (material.ConvUnit is not null && string.IsNullOrWhiteSpace(material.AlternateUom))
                material.AlternateUom = material.ConvUnit;
        }
        material.CompanyCode = Empty(record.CompanyCode);
        material.ValuationArea = Empty(record.ValuationArea);
        material.ValuationClass = Empty(record.ValuationClass);
        material.PriceControl = Empty(record.PriceControl);
        material.StandardPrice = record.StandardPrice;
        material.MovingAveragePrice = record.MovingAveragePrice;
        material.Currency = Empty(record.Currency)?.ToUpperInvariant();
        material.UnitCost = MaterialCosting.FromPriceControl(record.PriceControl, record.StandardPrice, record.MovingAveragePrice, record.UnitCost);
        material.AcquisitionSource = record.Source;
        material.SourceSystem = Empty(record.SourceSystem) ?? record.Source.ToString();
        material.SourceLastChangedAt = record.SourceLastChangedAt;
        material.UpdatedAt = DateTime.UtcNow;
        ApplyInventory(material, record);
        if (operational)
        {
            material.GovernanceStatus = MaterialGovernanceStatus.ACTIVE;
            material.Status = StatusKind.ACTIVE;
        }
    }

    public static void ApplyInventory(Material material, MaterialNormalizedRecord record)
    {
        var type = NormalizeInventoryType(record.InventoryType);
        if (!Enum.TryParse<InventoryItemType>(type, true, out var parsed))
            throw new RecipeManagementException("INVALID_INVENTORY_TYPE", "InventoryType must be STOCK, NON_STOCK, or SERVICE.");
        material.InventoryType = parsed;
        material.InventoryItem = parsed == InventoryItemType.SERVICE ? false : record.InventoryItem || parsed == InventoryItemType.STOCK;
        if (parsed == InventoryItemType.STOCK) material.InventoryItem = true;
        material.BatchManaged = record.BatchManaged;
        material.ExpiryManaged = record.ExpiryManaged;
        material.ShelfLifeDays = record.ExpiryManaged ? record.ShelfLifeDays : null;
        material.SerialManaged = record.SerialManaged;
    }

    public static string NormalizeInventoryType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "NON_STOCK";
        var key = value.Trim().ToUpperInvariant().Replace(' ', '_');
        if (key is "NONSTOCK") key = "NON_STOCK";
        if (key is not ("STOCK" or "NON_STOCK" or "SERVICE"))
            throw new RecipeManagementException("INVALID_INVENTORY_TYPE", "InventoryType must be STOCK, NON_STOCK, or SERVICE.");
        return key;
    }

    public static bool CanHoldStock(Material material) =>
        material.InventoryItem && material.InventoryType == InventoryItemType.STOCK;

    private static bool ParseBool(string? value) =>
        value is not null && value.Trim().ToUpperInvariant() is "TRUE" or "Y" or "YES" or "1";

    private static string? Get(IReadOnlyDictionary<string, string?> values, params string[] keys)
    {
        var normalized = values.ToDictionary(item => NormalizeHeader(item.Key), item => item.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
            if (normalized.TryGetValue(NormalizeHeader(key), out var value) && !string.IsNullOrWhiteSpace(value)) return value.Trim();
        return null;
    }

    private static string NormalizeHeader(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string? First(IEnumerable<string?> values) =>
        values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();

    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Format(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;
}

public static class MaterialCosting
{
    public static decimal? FromPriceControl(string? priceControl, decimal? standard, decimal? moving, decimal? unitCost)
    {
        var control = (priceControl ?? string.Empty).Trim().ToUpperInvariant();
        if (control is "S" or "STANDARD") return standard ?? unitCost;
        if (control is "V" or "MOVING" or "MOVINGAVERAGE") return moving ?? unitCost;
        return unitCost ?? standard ?? moving;
    }

    public static decimal? EffectiveUnitCost(Material material) =>
        material.GovernanceStatus is MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED
            ? AuthoritativeUnitPrice(material)
            : null;

    public static decimal? AuthoritativeUnitPrice(Material material) =>
        FromPriceControl(material.PriceControl, material.StandardPrice, material.MovingAveragePrice, material.UnitCost);

    public static bool HasApprovedUnitPrice(Material material) => EffectiveUnitCost(material) is not null;

    public static bool HasValidUnitPrice(Material material) => AuthoritativeUnitPrice(material) is not null;

    public static string PriceStatus(Material material, MaterialChangeRequest? pending, DateTime? priceEffectiveFrom = null, DateTime? priceEffectiveTo = null)
    {
        var now = DateTime.UtcNow;
        if (pending is { Status: MaterialGovernanceStatus.PENDING_APPROVAL })
            return "PRICE_PENDING_APPROVAL";
        if (material.GovernanceStatus == MaterialGovernanceStatus.REJECTED && EffectiveUnitCost(material) is null)
            return "PRICE_REJECTED";
        if (HasApprovedUnitPrice(material))
        {
            if (priceEffectiveTo is { } until && until < now)
                return "PRICE_EXPIRED";
            if (priceEffectiveFrom is { } from && from > now)
                return "PRICE_INVALID";
            return "PRICE_APPROVED";
        }
        if (AuthoritativeUnitPrice(material) is null)
            return "PRICE_MISSING";
        return "PRICE_INVALID";
    }

    public static string PriceStatusLabel(string status) => status switch
    {
        "PRICE_APPROVED" => "Approved",
        "PRICE_MISSING" => "Price Missing",
        "PRICE_PENDING_APPROVAL" => "Pending Approval",
        "PRICE_REJECTED" => "Rejected",
        "PRICE_EXPIRED" => "Expired",
        _ => "Invalid",
    };

    public static decimal? ProposedUnitPrice(MaterialChangeRequest? pending)
    {
        if (pending is null || string.IsNullOrWhiteSpace(pending.ProposedJson)) return null;
        try
        {
            var proposed = MaterialNormalized.Deserialize(pending.ProposedJson);
            return FromPriceControl(proposed.PriceControl, proposed.StandardPrice, proposed.MovingAveragePrice, proposed.UnitCost);
        }
        catch
        {
            return null;
        }
    }
}
