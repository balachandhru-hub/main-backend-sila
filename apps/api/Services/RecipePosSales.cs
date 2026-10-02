using System.Globalization;
using System.Text.Json;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class RecipePosSales
{
    public static readonly string[] Columns =
    [
        "BusinessDate", "TransactionID", "LineID", "POSCode", "Qty", "UOM", "OutletID", "Currency"
    ];

    public static string NormalizeOutlet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    public static byte[] Template() => ExcelOpenXml.WriteSheets(
    [
        new ExcelOpenXml.SheetWrite("INSTRUCTIONS", ["Field", "Required", "Notes"],
        [
            new() { ["Field"] = "BusinessDate", ["Required"] = "Yes", ["Notes"] = "Sale date (yyyy-MM-dd). Used for historical recipe version and SAP posting date." },
            new() { ["Field"] = "TransactionID", ["Required"] = "Yes", ["Notes"] = "POS ticket / check number." },
            new() { ["Field"] = "LineID", ["Required"] = "Yes", ["Notes"] = "Line number on the ticket. Together with BusinessDate + TransactionID this line is processed only once." },
            new() { ["Field"] = "POSCode", ["Required"] = "Yes", ["Notes"] = "Maps through POS Item Mapping to a menu item / recipe. Do not assume this is the Recipe ID." },
            new() { ["Field"] = "Qty", ["Required"] = "Yes", ["Notes"] = "Sold quantity. Must be non-zero. Recipe ingredients explode from Serving Qty." },
            new() { ["Field"] = "UOM", ["Required"] = "Yes", ["Notes"] = "Sale unit (GLASS, PORTION, EA, …)." },
            new() { ["Field"] = "OutletID", ["Required"] = "Yes", ["Notes"] = "POS outlet. SILA maps this to an inventory location. Do not enter Plant or Storage Location here." },
            new() { ["Field"] = "Currency", ["Required"] = "Yes", ["Notes"] = "Sale currency (AED, …)." },
        ]),
        new ExcelOpenXml.SheetWrite("SalesData", Columns,
        [
            Sample("2026-09-28", "TXN10001", "10", "CKTL001", "3", "GLASS", "POOL-BAR", "AED"),
            Sample("2026-09-28", "TXN10001", "20", "COKE001", "2", "EA", "POOL-BAR", "AED"),
            Sample("2026-09-28", "TXN10002", "10", "BURGER01", "1", "PORTION", "BEACH-BAR", "AED"),
        ]),
    ]);

    public static IReadOnlyList<PosSalesImportRow> Parse(Stream file)
    {
        IReadOnlyList<ExcelOpenXml.Sheet> sheets;
        try { sheets = ExcelOpenXml.ReadSheets(file); }
        catch { throw new RecipeManagementException("IMPORT_FILE_INVALID", "The file is not a valid Excel workbook."); }
        var sheet = ExcelOpenXml.FindSheet(sheets, "SalesData", "Data", "POS Sales", "Sales") ?? sheets[0];
        var rows = new List<PosSalesImportRow>();
        var index = 2;
        foreach (var values in ExcelOpenXml.ToDictionaries(sheet.Rows))
        {
            rows.Add(ParseRow(index++, values));
        }
        return rows;
    }

    public static PosSalesImportPreview Preview(Stream file, string fileName)
    {
        var rows = Parse(file);
        return new PosSalesImportPreview(fileName, rows.Count, rows.Count(item => item.IsValid), rows.Count(item => !item.IsValid), rows);
    }

    public static PosSalesImportRow ParseRow(int rowNumber, IReadOnlyDictionary<string, string?> values)
    {
        var errors = new List<string>();
        string? code = null;
        var date = RecipeExcel.Date(values, "BusinessDate", "Business Date", "SaleDate");
        var txn = RecipeExcel.Get(values, "TransactionID", "Transaction ID", "TxnID", "SourceTransactionId");
        var lineText = RecipeExcel.Get(values, "LineID", "Line ID", "SourceLineNumber", "LineNumber");
        var pos = RecipeExcel.Get(values, "POSCode", "POS Code", "PosItemCode", "ItemCode");
        var outlet = RecipeExcel.Get(values, "OutletID", "Outlet ID", "Outlet", "PosOutletCode");
        var uom = RecipeExcel.Get(values, "UOM", "Uom");
        var currency = RecipeExcel.Get(values, "Currency");
        var qty = RecipeExcel.Decimal(values, "Qty", "Quantity", "QuantitySold");
        int? lineId = null;
        if (string.IsNullOrWhiteSpace(lineText)) errors.Add("LineID is required.");
        else if (!int.TryParse(lineText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLine))
            errors.Add("LineID must be a whole number.");
        else lineId = parsedLine;
        if (date is null) { errors.Add("BusinessDate is required."); code ??= "INVALID_BUSINESS_DATE"; }
        if (string.IsNullOrWhiteSpace(txn)) errors.Add("TransactionID is required.");
        if (string.IsNullOrWhiteSpace(pos)) errors.Add("POSCode is required.");
        if (qty is null or 0) errors.Add("Qty must be a non-zero number.");
        if (string.IsNullOrWhiteSpace(uom)) errors.Add("UOM is required.");
        if (string.IsNullOrWhiteSpace(outlet)) errors.Add("OutletID is required.");
        if (string.IsNullOrWhiteSpace(currency)) errors.Add("Currency is required.");
        var status = errors.Count == 0 ? "READY" : "INVALID";
        return new PosSalesImportRow(rowNumber, errors.Count == 0, date, txn, lineId, pos, qty, uom, outlet, currency, errors, status, code, errors.Count == 0 ? null : string.Join("; ", errors));
    }

    public static string PackRaw(string? previous, Guid? inventoryLocationId, string? uom, string? currency, string? outletId)
    {
        return JsonSerializer.Serialize(new Dictionary<string, string?>
        {
            ["inventoryLocationId"] = (inventoryLocationId ?? InventoryLocationId(previous))?.ToString(),
            ["uom"] = uom ?? Read(previous, "uom"),
            ["currency"] = currency ?? Currency(previous),
            ["outletId"] = outletId ?? Read(previous, "outletId"),
        });
    }

    public static Guid? InventoryLocationId(string? raw)
    {
        var text = Read(raw, "inventoryLocationId");
        return Guid.TryParse(text, out var id) ? id : null;
    }

    public static bool IsOutletStoreOrVenue(InventoryLocation location) =>
        location.Status == StatusKind.ACTIVE &&
        location.LocationType is InventoryLocationType.OUTLET or InventoryLocationType.STORE or InventoryLocationType.VENUE;

    public static string LocationKindLabel(InventoryLocationType type) => type switch
    {
        InventoryLocationType.OUTLET => "Outlet",
        InventoryLocationType.STORE => "Store",
        InventoryLocationType.VENUE => "Venue",
        InventoryLocationType.PROPERTY => "Property",
        _ => type.ToString(),
    };

    public static InventoryLocation? FindStockingLocation(IEnumerable<InventoryLocation> locations, params string?[] codes)
    {
        var wanted = codes.Select(NormalizeOutlet).Where(item => item.Length > 0).Distinct().ToHashSet();
        if (wanted.Count == 0) return null;
        return locations.FirstOrDefault(item => IsOutletStoreOrVenue(item) && wanted.Contains(NormalizeOutlet(item.LocationCode)));
    }

    public static InventoryLocation? LocationForTransaction(IEnumerable<InventoryLocation> locations, RecipeConsumptionTransaction transaction)
    {
        var list = locations as IList<InventoryLocation> ?? locations.ToList();
        if (InventoryLocationId(transaction.RawReference) is { } stored)
        {
            var byId = list.FirstOrDefault(item => item.Id == stored);
            if (byId is not null && IsOutletStoreOrVenue(byId)) return byId;
        }
        return FindStockingLocation(list, transaction.OutletCode, transaction.PosOutletCode, Read(transaction.RawReference, "outletId"));
    }

    public static string? Currency(string? raw) => Read(raw, "currency");
    public static string? ReadUom(string? raw) => Read(raw, "uom");

    private static string? Read(string? raw, string name)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw[0] != '{') return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty(name, out var value) ? value.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, string?> Sample(string date, string txn, string line, string pos, string qty, string uom, string outlet, string currency) => new()
    {
        ["BusinessDate"] = date, ["TransactionID"] = txn, ["LineID"] = line, ["POSCode"] = pos, ["Qty"] = qty, ["UOM"] = uom, ["OutletID"] = outlet, ["Currency"] = currency,
    };
}
