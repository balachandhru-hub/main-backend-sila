using System.Globalization;
using System.Text.Json;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class RecipeSapUpdateStock
{
    public const string GoodsMovementCode = "03";
    public const string PositiveMovementType = "Z01";
    public const string NegativeMovementType = "Z02";
    public const string ConfigurationName = "FIVE_POS_UPDATE";
    public const IntegrationProcessType ProcessType = IntegrationProcessType.UPDATE_STOCK;

    private static readonly JsonSerializerOptions SapJson = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false,
    };

    public static string MovementType(decimal inventoryAdjustment) =>
        inventoryAdjustment > 0 ? PositiveMovementType : NegativeMovementType;

    public static string AbsoluteQuantity(decimal quantity) =>
        Math.Abs(quantity).ToString("0.000", CultureInfo.InvariantCulture);

    public static string BusinessDateTime(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00";

    public static string BuildRequestJson(DateOnly businessDate, IReadOnlyList<RecipeSapStockItem> items)
    {
        var body = new Dictionary<string, object?>
        {
            ["PostingDate"] = BusinessDateTime(businessDate),
            ["DocumentDate"] = BusinessDateTime(businessDate),
            ["GoodsMovementCode"] = GoodsMovementCode,
            ["to_MaterialDocumentItem"] = items.Select(item => new Dictionary<string, string>
            {
                ["Plant"] = item.Plant,
                ["StorageLocation"] = item.StorageLocation,
                ["Material"] = item.Material,
                ["GoodsMovementType"] = item.GoodsMovementType,
                ["QuantityInEntryUnit"] = AbsoluteQuantity(item.Quantity),
                ["EntryUnit"] = item.EntryUnit,
            }).ToList(),
        };
        return JsonSerializer.Serialize(body, SapJson);
    }

    public static IReadOnlyList<RecipeSapStockItem> Aggregate(IEnumerable<RecipeSapStockItem> items) =>
        items
            .GroupBy(item => (item.BusinessDate, item.Plant, item.StorageLocation, item.Material, item.EntryUnit, item.GoodsMovementType))
            .Select(group => group.First() with { Quantity = group.Sum(item => Math.Abs(item.Quantity)), TransactionIds = group.SelectMany(item => item.TransactionIds).Distinct().ToArray() })
            .ToList();

    public static string? FirstNonEmpty(params string?[] values) =>
        values.Select(item => item?.Trim()).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));

    public static string? DistinctMapping(string? mapped, string? locationCode)
    {
        var value = mapped?.Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (string.Equals(RecipePosSales.NormalizeOutlet(value), RecipePosSales.NormalizeOutlet(locationCode), StringComparison.Ordinal))
            return null;
        return value;
    }

    public static (string? Plant, string? StorageLocation) ResolvePlantStorage(
        PosOutletMapping? mapping, InventoryLocation location, string? propertyCostCenter, string? configurationPlant)
    {
        var plant = FirstNonEmpty(
            DistinctMapping(mapping?.PlantCode, location.LocationCode),
            location.CostCenter,
            location.PropertyLocation?.CostCenter,
            propertyCostCenter,
            configurationPlant);
        var storage = FirstNonEmpty(
            DistinctMapping(mapping?.StorageLocationCode, location.LocationCode),
            mapping?.StorageLocationCode,
            location.LocationCode);
        return (plant, storage);
    }

    public static (string? MaterialDocument, string? DocumentYear) ReadDocument(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return (null, null);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("d", out var d)) root = d;
            return (Read(root, "MaterialDocument", "materialDocument", "MaterialDocumentNumber"),
                Read(root, "MaterialDocumentYear", "DocumentYear", "documentYear"));
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    public static (string Step1Status, string Step1Message, string Step2Status, string Step2Message) TrackerSteps(
        RecipeConsumptionTransaction item, RecipeInventoryPosting? posting, InventoryLocation? location)
    {
        var locationLabel = location is null
            ? item.OutletCode ?? item.PosOutletCode
            : $"{RecipePosSales.LocationKindLabel(location.LocationType)} {location.LocationName} ({location.LocationCode})";
        var erpFailed = item.FailedStep is "S4_POSTING" or "S4_RESPONSE" or "SAP_POSTING";
        var step1Done = item.Status is RecipeTransactionStatus.READY_TO_POST or RecipeTransactionStatus.POSTING
            or RecipeTransactionStatus.POSTED or RecipeTransactionStatus.POSTING_UNKNOWN
            || (item.Status == RecipeTransactionStatus.FAILED && erpFailed);
        var step1Status = step1Done ? "UPDATED" : "FAILED";
        var step1Message = step1Status switch
        {
            "UPDATED" => $"Step 1 complete. Local inventory was updated at {locationLabel}.",
            _ => item.FailureMessage ?? "Step 1 failed. The sale was received but local inventory was not updated.",
        };
        var response = Trim(posting?.ResponseJson);
        string step2Status;
        string step2Message;
        if (item.Status == RecipeTransactionStatus.POSTED)
        {
            step2Status = "SUCCESS";
            step2Message = string.IsNullOrWhiteSpace(item.ExternalReference ?? posting?.MaterialDocument)
                ? "Step 2 complete. SAP accepted the inventory adjustment."
                : $"Step 2 complete. SAP material document {item.ExternalReference ?? posting?.MaterialDocument}.";
        }
        else if (item.Status == RecipeTransactionStatus.POSTING)
        {
            step2Status = "PENDING";
            step2Message = "Step 2 is calling the ERP UPDATE_STOCK API.";
        }
        else if (step1Done)
        {
            step2Status = "FAILED";
            step2Message = item.Status == RecipeTransactionStatus.POSTING_UNKNOWN
                ? Friendly("POSTING_UNKNOWN", posting?.ErrorMessage ?? item.FailureMessage)
                : posting?.ErrorMessage ?? item.FailureMessage ?? "Step 2 failed. Local inventory was updated but ERP UPDATE_STOCK did not complete.";
        }
        else
        {
            step2Status = "NOT_STARTED";
            step2Message = "Step 2 was not called because local inventory was not updated.";
        }
        if (!string.IsNullOrWhiteSpace(response) && step2Status is "SUCCESS" or "FAILED" or "UNKNOWN")
            step2Message = $"{step2Message}\n\nERP response:\n{response}";
        return (step1Status, step1Message, step2Status, step2Message);
    }

    public static bool CanReprocess(RecipeTransactionStatus status) =>
        status is not RecipeTransactionStatus.POSTED and not RecipeTransactionStatus.POSTING;

    private static string? Trim(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : json.Length > 4000 ? json[..4000] : json;

    public static string Friendly(string? code, string? message) => code switch
    {
        "UNMAPPED_POS_CODE" or "POS_ITEM_NOT_MAPPED" => "POS item not mapped",
        "OUTLET_MAPPING_MISSING" or "OUTLET_LOCATION_MISSING" => "Outlet not mapped to an inventory location",
        "OUTLET_INACTIVE" => "Outlet inventory location is not active",
        "CONSUMPTION_DISABLED" => "Consumption is not enabled for this inventory location",
        "NO_ACTIVE_RECIPE_VERSION" or "RECIPE_NOT_APPROVED" or "RECIPE_VERSION_MISSING" => "No approved recipe valid for business date",
        "MATERIAL_INACTIVE" => "Material inactive",
        "MATERIAL_NOT_INVENTORY" or "NO_STOCK_ITEMS" => "Material is not inventory enabled",
        "UOM_CONVERSION_MISSING" => "Material UOM conversion missing",
        "PLANT_MAPPING_MISSING" => "Plant mapping missing",
        "STORAGE_LOCATION_MAPPING_MISSING" => "Storage Location mapping missing",
        "UPDATE_STOCK_ROUTE_NOT_CONFIGURED" or "INTEGRATION_ROUTE_NOT_FOUND" => "UPDATE_STOCK API route not configured",
        "SAP_POST_FAILED" or "ERP_POST_FAILED" => "SAP posting failed",
        "POSTING_UNKNOWN" or "ERP_POST_UNKNOWN" => "SAP posting confirmation unknown",
        "DUPLICATE" => "This sales line was already processed",
        _ => string.IsNullOrWhiteSpace(message) ? "Sales line could not be processed" : message,
    };

    private static string? Read(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                return value.ToString();
        }
        return null;
    }
}

public sealed record RecipeSapStockItem(
    DateOnly BusinessDate,
    string Plant,
    string StorageLocation,
    string Material,
    string GoodsMovementType,
    decimal Quantity,
    string EntryUnit,
    IReadOnlyList<Guid> TransactionIds);
