using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed partial class IntegrationService
{
    private static readonly string[] PurchaseOrderHeaderColumns =
    [
        "EXTERNAL_ID", "PO_NUMBER", "PURCHASE_ORDER_TYPE", "SUPPLIER_CODE", "SUPPLIER_NAME", "COMPANY_CODE", "CURRENCY",
        "TOTAL_AMOUNT", "PO_STATUS", "PO_DATE", "PURCHASING_ORGANIZATION", "PURCHASING_GROUP", "PAYMENT_TERMS", "PO_CATEGORY",
        "TOTAL_NET_AMOUNT", "TOTAL_TAX_AMOUNT", "DELIVERY_DATE", "SOURCE_LAST_CHANGED_AT",
    ];

    private static readonly string[] PurchaseOrderItemColumns =
    [
        "LINE_NUMBER", "MATERIAL_CODE", "DESCRIPTION", "ORDERED_QUANTITY", "UOM", "ITEM_AMOUNT", "TAX_CODE",
        "GOODS_RECEIPT_EXPECTED", "MATERIAL_GROUP", "PLANT", "STORAGE_LOCATION", "ITEM_CATEGORY", "ACCOUNT_ASSIGNMENT_CATEGORY",
        "RECEIVED_QUANTITY", "OPEN_QUANTITY", "UNIT_PRICE", "PRICE_QUANTITY", "TAX_AMOUNT", "GROSS_ITEM_AMOUNT",
        "INVOICE_EXPECTED", "DELIVERY_COMPLETED", "DELETION_INDICATOR", "ITEM_STATUS", "ITEM_CURRENCY",
    ];

    public async Task<ApiIntegrationConfiguration> GetOrCreateExcelConfigurationAsync(
        Guid organizationId, string entityCode, IntegrationImportKind kind, CancellationToken cancellationToken)
    {
        var process = kind == IntegrationImportKind.SUPPLIERS ? IntegrationProcessType.GET_SUPPLIER : IntegrationProcessType.GET_PO;
        var existing = await db.ApiIntegrationConfigurations.FirstOrDefaultAsync(item =>
            item.OrganizationId == organizationId && item.ProcessType == process &&
            (item.EntityCode == entityCode || item.EntityCode == "ALL"), cancellationToken);
        if (existing is not null) return existing;
        var saved = await SaveAsync(organizationId, null, new IntegrationConfigurationInput
        {
            Name = $"Excel {kind} upload",
            EntityCode = string.IsNullOrWhiteSpace(entityCode) ? "ALL" : entityCode,
            ProcessType = process,
            Protocol = IntegrationProtocol.REST,
            BaseUrl = "https://local.excel.silame",
            ResourcePath = "excel",
            AuthenticationType = IntegrationAuthenticationType.NONE,
        }, cancellationToken);
        return await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id, cancellationToken);
    }

    public async Task<IntegrationImportPreviewResponse> PreviewMasterImportAsync(
        Guid organizationId, string entityCode, IntegrationImportKind kind, Stream file, string fileName, CancellationToken cancellationToken)
    {
        var config = await GetOrCreateExcelConfigurationAsync(organizationId, entityCode, kind, cancellationToken);
        return await PreviewImportAsync(organizationId, config.Id, kind, file, fileName, cancellationToken);
    }

    public async Task<IntegrationImportCommitResponse> CommitMasterImportAsync(
        Guid organizationId, string entityCode, IntegrationImportCommitInput input, CancellationToken cancellationToken)
    {
        var config = await GetOrCreateExcelConfigurationAsync(organizationId, entityCode, input.Kind, cancellationToken);
        return await CommitImportAsync(organizationId, config.Id, input, cancellationToken);
    }

    private async Task<Supplier?> FindSupplierAsync(Guid organizationId, string entityCode, string supplierCode, CancellationToken cancellationToken)
    {
        var matches = await db.Suppliers
            .Where(item => item.OrganizationId == organizationId && item.SupplierCode == supplierCode && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        return matches.FirstOrDefault(item => item.EntityCode == entityCode)
            ?? (entityCode == "ALL" ? matches.FirstOrDefault() : matches.FirstOrDefault(item => item.EntityCode == "ALL"));
    }

    private async Task<List<IntegrationImportRowResponse>> ClassifyImportRowsAsync(
        Guid organizationId, string entityCode, IntegrationImportKind kind, List<IntegrationImportRowResponse> rows, CancellationToken cancellationToken)
    {
        var supplierCodes = rows.Select(row => row.Values.TryGetValue("SUPPLIER_CODE", out var value) ? value : null)
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var suppliers = await db.Suppliers.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && supplierCodes.Contains(item.SupplierCode) && !item.IsDeleted)
            .Select(item => new { item.SupplierCode, item.EntityCode, item.Name, item.NormalizedName })
            .ToListAsync(cancellationToken);
        var existingCodes = suppliers
            .Where(item => item.EntityCode == entityCode || entityCode == "ALL" || item.EntityCode == "ALL")
            .Select(item => item.SupplierCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        HashSet<string> existingPos = [];
        HashSet<string> existingItems = [];
        if (kind == IntegrationImportKind.PURCHASE_ORDERS)
        {
            var poNumbers = rows.Select(row => row.Values.TryGetValue("PO_NUMBER", out var value) ? value : null)
                .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).Distinct().ToList();
            var pos = await db.PurchaseOrders.AsNoTracking().Include(item => item.Items)
                .Where(item => item.OrganizationId == organizationId && poNumbers.Contains(item.PoNumber) &&
                    (item.EntityCode == entityCode || entityCode == "ALL"))
                .ToListAsync(cancellationToken);
            existingPos = pos.Select(item => item.PoNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
            existingItems = pos.SelectMany(po => po.Items.Select(item => $"{po.PoNumber}|{item.LineNumber}")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var errors = row.Errors.ToList();
            var codes = (row.ErrorCodes ?? []).ToList();
            if (kind == IntegrationImportKind.PURCHASE_ORDERS &&
                row.Values.TryGetValue("SUPPLIER_CODE", out var supplierCode) &&
                !string.IsNullOrWhiteSpace(supplierCode) &&
                !existingCodes.Contains(supplierCode))
            {
                errors.Add("Supplier does not exist in Supplier Master for this organization and entity.");
                codes.Add("SUPPLIER_NOT_FOUND");
            }

            var action = "NEW";
            if (kind == IntegrationImportKind.SUPPLIERS)
            {
                var code = row.Values.TryGetValue("SUPPLIER_CODE", out var value) ? value : null;
                if (!string.IsNullOrWhiteSpace(code) && existingCodes.Contains(code)) action = "UPDATE";
            }
            else
            {
                var po = row.Values.TryGetValue("PO_NUMBER", out var poNumber) ? poNumber : null;
                var line = row.Values.TryGetValue("LINE_NUMBER", out var lineNumber) ? lineNumber : null;
                if (!string.IsNullOrWhiteSpace(po) && !string.IsNullOrWhiteSpace(line) && existingItems.Contains($"{po}|{line}")) action = "UPDATE";
                else if (!string.IsNullOrWhiteSpace(po) && existingPos.Contains(po) && string.IsNullOrWhiteSpace(line)) action = "UPDATE";
            }

            rows[index] = row with
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                ErrorCodes = codes,
                Action = action,
            };
        }
        return rows;
    }

    private static IReadOnlyList<ApiFieldMapping> ItemImportMappings() =>
        PurchaseOrderItemColumns.Select(column =>
        {
            var property = column == "UOM" ? "Uom" : ToPascal(column);
            if (column == "ITEM_CURRENCY") property = "Currency";
            return new ApiFieldMapping
            {
                Id = Guid.NewGuid(), ConfigurationId = Guid.Empty, SourceField = property,
                TargetField = $"PurchaseOrderItem.{property}", NullPolicy = IntegrationNullPolicy.IGNORE_NULL,
                IsValidated = true, UpdatedAt = DateTime.UtcNow,
            };
        }).ToList();

    private static JsonElement ToCanonicalPurchaseOrderRecord(IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
    {
        var fields = new Dictionary<string, string?>();
        foreach (var column in PurchaseOrderHeaderColumns)
        {
            var property = column switch
            {
                "SUPPLIER_CODE" => "SupplierCode",
                "SUPPLIER_NAME" => "SupplierName",
                "PO_STATUS" => "PoStatus",
                _ => ToPascal(column),
            };
            fields[property] = FirstNonEmpty(rows, column);
        }
        var items = rows.Where(HasPoItemData).Select(row =>
        {
            var item = new Dictionary<string, string?>();
            foreach (var column in PurchaseOrderItemColumns)
            {
                var property = column == "UOM" ? "Uom" : column == "ITEM_CURRENCY" ? "Currency" : ToPascal(column);
                item[property] = row.TryGetValue(column, out var value) ? value : null;
            }
            return item;
        }).ToList();
        return JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["PurchaseOrder"] = fields,
            ["Items"] = items,
        })).RootElement.Clone();
    }

    private static string? FirstNonEmpty(IReadOnlyList<IReadOnlyDictionary<string, string?>> rows, string column)
    {
        foreach (var row in rows)
        {
            if (row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static bool HasPoItemData(IReadOnlyDictionary<string, string?> values) =>
        PurchaseOrderItemColumns.Any(column => values.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value) && column != "GOODS_RECEIPT_EXPECTED");

    private static string DuplicateKey(IReadOnlyDictionary<string, string?> values)
    {
        var po = values.TryGetValue("PO_NUMBER", out var poNumber) ? poNumber ?? string.Empty : string.Empty;
        var line = values.TryGetValue("LINE_NUMBER", out var lineNumber) ? lineNumber : null;
        return string.IsNullOrWhiteSpace(line) ? po : $"{po}|{line}";
    }

    private static string CanonicalImportKey(string header, IntegrationImportKind kind)
    {
        var normalized = NormalizeHeader(header);
        var aliases = kind == IntegrationImportKind.SUPPLIERS ? SupplierHeaderAliases : PurchaseOrderHeaderAliases;
        return aliases.TryGetValue(normalized, out var canonical) ? canonical : header.Trim().ToUpperInvariant();
    }

    private static readonly Dictionary<string, string> SupplierHeaderAliases = new()
    {
        ["SUPPLIERID"] = "SUPPLIER_CODE", ["SUPPLIERCODE"] = "SUPPLIER_CODE", ["SUPPLIERNAME"] = "NAME",
        ["NAME"] = "NAME", ["BUSINESSPARTNERID"] = "BUSINESS_PARTNER_ID", ["SUPPLIERLEGALNAME"] = "LEGAL_NAME",
        ["LEGALNAME"] = "LEGAL_NAME", ["TAXNUMBER"] = "TAX_NUMBER", ["TRN"] = "TRN", ["COUNTRY"] = "COUNTRY",
        ["CITY"] = "CITY", ["POSTALCODE"] = "POSTAL_CODE", ["STREET"] = "STREET", ["EMAIL"] = "EMAIL", ["PHONE"] = "PHONE",
        ["PURCHASINGORGANIZATION"] = "PURCHASING_ORGANIZATION", ["COMPANYCODE"] = "COMPANY_CODE", ["CURRENCY"] = "CURRENCY",
        ["PAYMENTTERMS"] = "PAYMENT_TERMS", ["ISBLOCKED"] = "IS_BLOCKED", ["ISACTIVE"] = "IS_ACTIVE",
        ["SOURCELASTCHANGEDAT"] = "SOURCE_LAST_CHANGED_AT", ["EXTERNALID"] = "EXTERNAL_ID",
    };

    private static readonly Dictionary<string, string> PurchaseOrderHeaderAliases = new()
    {
        ["PURCHASEORDER"] = "PO_NUMBER", ["PONUMBER"] = "PO_NUMBER", ["PURCHASEORDERTYPE"] = "PURCHASE_ORDER_TYPE",
        ["SUPPLIERID"] = "SUPPLIER_CODE", ["SUPPLIERCODE"] = "SUPPLIER_CODE", ["SUPPLIERNAME"] = "SUPPLIER_NAME",
        ["COMPANYCODE"] = "COMPANY_CODE", ["CURRENCY"] = "CURRENCY", ["TOTALAMOUNT"] = "TOTAL_AMOUNT",
        ["POSTATUS"] = "PO_STATUS", ["STATUS"] = "PO_STATUS", ["PURCHASEORDERDATE"] = "PO_DATE", ["PODATE"] = "PO_DATE",
        ["PURCHASINGORGANIZATION"] = "PURCHASING_ORGANIZATION", ["PURCHASINGGROUP"] = "PURCHASING_GROUP",
        ["PAYMENTTERMS"] = "PAYMENT_TERMS", ["POCATEGORY"] = "PO_CATEGORY", ["TOTALNETAMOUNT"] = "TOTAL_NET_AMOUNT",
        ["TOTALTAXAMOUNT"] = "TOTAL_TAX_AMOUNT", ["DELIVERYDATE"] = "DELIVERY_DATE", ["SOURCELASTCHANGEDAT"] = "SOURCE_LAST_CHANGED_AT",
        ["EXTERNALID"] = "EXTERNAL_ID", ["PURCHASEORDERITEM"] = "LINE_NUMBER", ["POITEM"] = "LINE_NUMBER",
        ["LINENUMBER"] = "LINE_NUMBER", ["MATERIAL"] = "MATERIAL_CODE", ["MATERIALCODE"] = "MATERIAL_CODE",
        ["MATERIALDESCRIPTION"] = "DESCRIPTION", ["DESCRIPTION"] = "DESCRIPTION", ["ORDERQUANTITY"] = "ORDERED_QUANTITY",
        ["ORDEREDQUANTITY"] = "ORDERED_QUANTITY", ["UOM"] = "UOM", ["ITEMAMOUNT"] = "ITEM_AMOUNT", ["TAXCODE"] = "TAX_CODE",
        ["GOODSRECEIPTEXPECTED"] = "GOODS_RECEIPT_EXPECTED", ["MATERIALGROUP"] = "MATERIAL_GROUP", ["PLANT"] = "PLANT",
        ["STORAGELOCATION"] = "STORAGE_LOCATION", ["ITEMCATEGORY"] = "ITEM_CATEGORY",
        ["ACCOUNTASSIGNMENTCATEGORY"] = "ACCOUNT_ASSIGNMENT_CATEGORY", ["RECEIVEDQUANTITY"] = "RECEIVED_QUANTITY",
        ["OPENQUANTITY"] = "OPEN_QUANTITY", ["NETPRICE"] = "UNIT_PRICE", ["UNITPRICE"] = "UNIT_PRICE",
        ["PRICEQUANTITY"] = "PRICE_QUANTITY", ["TAXAMOUNT"] = "TAX_AMOUNT", ["GROSSITEMAMOUNT"] = "GROSS_ITEM_AMOUNT",
        ["INVOICEEXPECTED"] = "INVOICE_EXPECTED", ["DELIVERYCOMPLETED"] = "DELIVERY_COMPLETED",
        ["DELETIONINDICATOR"] = "DELETION_INDICATOR", ["ITEMSTATUS"] = "ITEM_STATUS", ["ITEMCURRENCY"] = "ITEM_CURRENCY",
    };

    private static void AddError(List<string> errors, string code, string message) => errors.Add($"[{code}] {message}");

    private static string? ErrorCodeOf(string error)
    {
        if (error.StartsWith('[') && error.Contains(']'))
            return error[1..error.IndexOf(']')];
        return error.Contains("Duplicate key", StringComparison.OrdinalIgnoreCase) ? "DUPLICATE_KEY" : null;
    }

    private static string ErrorMessageOf(string error)
    {
        if (error.StartsWith('[') && error.Contains(']'))
            return error[(error.IndexOf(']') + 1)..].Trim();
        return error;
    }

    private static bool TryParsePoStatus(string value, out PurchaseOrderStatus status)
    {
        var normalized = Regex.Replace(value.Trim().ToUpperInvariant(), @"[\s-]+", "_");
        if (normalized is "CANCELED") normalized = "CANCELLED";
        if (normalized is "PARTIAL" or "PARTIALLYRECEIVED") normalized = "PARTIALLY_RECEIVED";
        return Enum.TryParse(normalized, true, out status) &&
               status is PurchaseOrderStatus.OPEN or PurchaseOrderStatus.PARTIALLY_RECEIVED or PurchaseOrderStatus.CLOSED or PurchaseOrderStatus.CANCELLED;
    }

    private static bool? ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToUpperInvariant();
        return normalized is "TRUE" or "YES" or "Y" or "1" ? true : normalized is "FALSE" or "NO" or "N" or "0" ? false : null;
    }

    private sealed record WorkbookSheet(string Name, List<List<string>> Rows);

    private static List<WorkbookSheet> ReadWorkbookSheets(ZipArchive archive)
    {
        var shared = archive.GetEntry("xl/sharedStrings.xml") is { } sharedEntry
            ? XDocument.Load(sharedEntry.Open()).Descendants().Where(item => item.Name.LocalName == "si")
                .Select(item => string.Concat(item.Descendants().Where(text => text.Name.LocalName == "t").Select(text => text.Value))).ToList()
            : [];
        var workbook = archive.GetEntry("xl/workbook.xml") ?? throw new InvalidDataException();
        var rels = archive.GetEntry("xl/_rels/workbook.xml.rels");
        var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (rels is not null)
        {
            foreach (var relationship in XDocument.Load(rels.Open()).Descendants().Where(item => item.Name.LocalName == "Relationship"))
            {
                var id = relationship.Attribute("Id")?.Value;
                var target = relationship.Attribute("Target")?.Value;
                if (id is not null && target is not null) targets[id] = target.Replace('\\', '/');
            }
        }

        var sheets = new List<WorkbookSheet>();
        foreach (var sheet in XDocument.Load(workbook.Open()).Descendants().Where(item => item.Name.LocalName == "sheet"))
        {
            var name = sheet.Attribute("name")?.Value ?? "Data";
            if (name.Equals("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase)) continue;
            var relId = sheet.Attributes().FirstOrDefault(item => item.Name.LocalName == "id")?.Value;
            var path = relId is not null && targets.TryGetValue(relId, out var target)
                ? "xl/" + target.TrimStart('/')
                : $"xl/worksheets/sheet{sheets.Count + 1}.xml";
            if (path.StartsWith("xl/xl/", StringComparison.OrdinalIgnoreCase)) path = path[3..];
            var entry = archive.GetEntry(path) ?? archive.GetEntry($"xl/worksheets/sheet{sheets.Count + 1}.xml");
            if (entry is null) continue;
            var rows = XDocument.Load(entry.Open()).Descendants().Where(item => item.Name.LocalName == "row").Select(row =>
                row.Elements().Where(item => item.Name.LocalName == "c").Select(cell =>
                {
                    var type = cell.Attribute("t")?.Value;
                    return type == "inlineStr"
                        ? string.Concat(cell.Descendants().Where(item => item.Name.LocalName == "t").Select(item => item.Value))
                        : type == "s" && int.TryParse(cell.Elements().FirstOrDefault(item => item.Name.LocalName == "v")?.Value, out var index) && index < shared.Count
                            ? shared[index]
                            : cell.Elements().FirstOrDefault(item => item.Name.LocalName == "v")?.Value ?? string.Empty;
                }).ToList()).ToList();
            sheets.Add(new WorkbookSheet(name, rows));
        }

        if (sheets.Count == 0)
        {
            var fallback = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException();
            var rows = XDocument.Load(fallback.Open()).Descendants().Where(item => item.Name.LocalName == "row").Select(row =>
                row.Elements().Where(item => item.Name.LocalName == "c").Select(cell =>
                    string.Concat(cell.Descendants().Where(item => item.Name.LocalName == "t").Select(item => item.Value))).ToList()).ToList();
            sheets.Add(new WorkbookSheet("Data", rows));
        }
        return sheets;
    }

    private static List<Dictionary<string, string?>> RowsToDictionaries(List<List<string>> rows)
    {
        if (rows.Count == 0) return [];
        var headers = rows[0].ToList();
        return rows.Skip(1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))).Select(row =>
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
                result[headers[index]] = index < row.Count ? row[index] : null;
            return result;
        }).ToList();
    }

    private static byte[] BuildMasterTemplate(IntegrationImportKind kind)
    {
        if (kind == IntegrationImportKind.SUPPLIERS)
        {
            return BuildMultiSheetWorkbook(
            [
                ("SUPPLIERS", [
                    "SupplierId *", "SupplierName *", "BusinessPartnerId", "SupplierLegalName", "TaxNumber", "TRN",
                    "Country", "City", "PostalCode", "Street", "Email", "Phone", "PurchasingOrganization", "CompanyCode",
                    "Currency", "PaymentTerms", "IsBlocked", "IsActive", "SourceLastChangedAt",
                ], []),
                ("INSTRUCTIONS", ["Field", "Required", "Notes"],
                [
                    ["SupplierId", "YES", "Business key with Organization + Entity. Do not use Supplier Name as the unique ID."],
                    ["SupplierName", "YES", "Display name used for OCR matching."],
                    ["All other columns", "NO", "Optional master-data attributes. Blank cells are ignored."],
                    ["Confirm import", "—", "Preview does not write to the database. Confirm import performs one transactional upsert."],
                ]),
            ]);
        }

        return BuildMultiSheetWorkbook(
        [
            ("PO_HEADERS", [
                "PurchaseOrder *", "PurchaseOrderType *", "SupplierId *", "CompanyCode *", "Currency *", "TotalAmount *", "POStatus *",
                "SupplierName", "PurchaseOrderDate", "PurchasingOrganization", "PurchasingGroup", "PaymentTerms", "POCategory",
                "TotalNetAmount", "TotalTaxAmount", "SourceLastChangedAt",
            ], []),
            ("PO_ITEMS", [
                "PurchaseOrder *", "PurchaseOrderItem *", "Material **", "MaterialDescription **", "OrderQuantity *", "UOM *",
                "Currency *", "ItemAmount *", "TaxCode *", "GoodsReceiptExpected *",
                "MaterialGroup", "Plant", "StorageLocation", "ItemCategory", "AccountAssignmentCategory", "ReceivedQuantity",
                "OpenQuantity", "NetPrice", "PriceQuantity", "TaxAmount", "GrossItemAmount", "InvoiceExpected",
                "DeliveryCompleted", "DeletionIndicator", "ItemStatus", "SourceLastChangedAt",
            ], []),
            ("INSTRUCTIONS", ["Field", "Required", "Notes"],
            [
                ["Organization / Entity", "YES", "Selected in Cloud before upload. Not columns in this workbook."],
                ["PO header * fields", "YES", "PurchaseOrder, PurchaseOrderType, SupplierId, CompanyCode, Currency, TotalAmount, POStatus."],
                ["PO item * fields", "YES", "PurchaseOrder, PurchaseOrderItem, OrderQuantity, UOM, Currency, ItemAmount, TaxCode, GoodsReceiptExpected."],
                ["Material ** / MaterialDescription **", "ONE REQUIRED", "Material or Item Description is required for every PO item. Code: PO_ITEM_IDENTIFICATION_REQUIRED."],
                ["SupplierId", "YES", "Must already exist in Supplier Master. Missing suppliers return SUPPLIER_NOT_FOUND and are not created."],
                ["Currency", "YES", "ISO-4217, uppercase. Header and item currency must match. Code: PO_CURRENCY_MISMATCH."],
                ["SourceLastChangedAt", "NO", "Ariba unique name (for example EP23721) or a timestamp. Required for Ariba GRN posting as ReceivableId."],
            ]),
        ]);
    }

    public static byte[] CreateWorkbook(IReadOnlyList<(string Name, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows)> sheets) =>
        BuildMultiSheetWorkbook(sheets);

    private static byte[] BuildMultiSheetWorkbook(IReadOnlyList<(string Name, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows)> sheets)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            var overrides = string.Concat(sheets.Select((_, index) =>
                $"<Override PartName=\"/xl/worksheets/sheet{index + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"));
            AddZipEntry(archive, "[Content_Types].xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>{overrides}</Types>
                """);
            AddZipEntry(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            var sheetRefs = string.Concat(sheets.Select((sheet, index) =>
                $"<sheet name=\"{SecurityEscape(sheet.Name)}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>"));
            AddZipEntry(archive, "xl/workbook.xml", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>{sheetRefs}</sheets></workbook>""");
            var rels = string.Concat(sheets.Select((_, index) =>
                $"<Relationship Id=\"rId{index + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{index + 1}.xml\"/>"));
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{rels}</Relationships>""");
            for (var sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                var sheet = sheets[sheetIndex];
                var xml = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
                AppendSpreadsheetRow(xml, sheet.Columns.Select(item => (string?)item).ToList(), 1);
                for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
                    AppendSpreadsheetRow(xml, sheet.Rows[rowIndex].Select(item => (string?)item).ToList(), rowIndex + 2);
                xml.Append("</sheetData></worksheet>");
                AddZipEntry(archive, $"xl/worksheets/sheet{sheetIndex + 1}.xml", xml.ToString());
            }
        }
        return output.ToArray();
    }

    private static string SecurityEscape(string value) => System.Security.SecurityElement.Escape(value) ?? value;
}
