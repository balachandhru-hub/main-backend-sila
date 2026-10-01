using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;

namespace Operations.Application.Services.Integration
{
    public interface IIntegrationSpreadsheetEngine
    {
        List<string> Columns(IntegrationImportKind kind);

        /// <summary>Reads the data rows of an .xlsx or .csv upload (header row = column names).</summary>
        List<Dictionary<string, string?>> Read(byte[] content, string fileName);

        /// <summary>Normalizes and validates rows. Nothing is written: invalid rows carry their errors.</summary>
        List<IntegrationImportRowResponseDto> Normalize(IntegrationImportKind kind, IEnumerable<Dictionary<string, string?>> rows);

        byte[] BuildSpreadsheet(List<string> columns, List<Dictionary<string, string?>> rows);

        byte[] BuildCorrectionReport(IntegrationImportKind kind, List<IntegrationImportRowResponseDto> rows);

        /// <summary>Identity mappings (target field = source field) used to import a canonical row.</summary>
        List<ApiFieldMapping> ImportMappings(IntegrationImportKind kind);

        /// <summary>A canonical row as the JSON record the import path reads.</summary>
        JsonElement ToCanonicalRecord(IntegrationImportKind kind, Dictionary<string, string?> values);
    }

    /// <summary>
    /// Excel / CSV engine of the data-update screen: template, export, import preview and correction
    /// report. Workbooks are written and read directly as Open XML, without a spreadsheet library.
    /// </summary>
    public class IntegrationSpreadsheetEngine : IIntegrationSpreadsheetEngine
    {
        private static readonly List<string> SupplierColumns = new List<string>
        {
            "SUPPLIER_CODE", "NAME", "LEGAL_NAME", "TAX_NUMBER", "EMAIL", "PHONE", "COUNTRY", "CURRENCY", "EXTERNAL_ID"
        };

        private static readonly List<string> PurchaseOrderColumns = new List<string>
        {
            "EXTERNAL_ID", "PO_NUMBER", "SUPPLIER_CODE", "SUPPLIER_NAME", "CURRENCY", "COMPANY_CODE", "PURCHASE_ORDER_TYPE",
            "TOTAL_NET_AMOUNT", "TOTAL_TAX_AMOUNT", "TOTAL_AMOUNT", "PO_DATE", "DELIVERY_DATE", "SOURCE_LAST_CHANGED_AT"
        };

        public List<string> Columns(IntegrationImportKind kind)
        {
            return kind == IntegrationImportKind.SUPPLIERS ? SupplierColumns.ToList() : PurchaseOrderColumns.ToList();
        }

        public List<Dictionary<string, string?>> Read(byte[] content, string fileName)
        {
            if (content.Length > Common.MAX_IMPORT_SIZE)
            {
                throw new IntegrationException("IMPORT_TOO_LARGE", "The spreadsheet must be smaller than 10 MB.");
            }

            return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                ? ParseCsv(Encoding.UTF8.GetString(content))
                : ParseXlsx(content);
        }

        public List<IntegrationImportRowResponseDto> Normalize(IntegrationImportKind kind, IEnumerable<Dictionary<string, string?>> rows)
        {
            List<IntegrationImportRowResponseDto> normalized = rows.Select((row, index) =>
            {
                Dictionary<string, string?> values = NormalizeRow(kind, row, out List<string> errors);
                return new IntegrationImportRowResponseDto { RowNumber = index + 2, IsValid = errors.Count == 0, Values = values, Errors = errors };
            }).ToList();

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string keyColumn = kind == IntegrationImportKind.SUPPLIERS ? "SUPPLIER_CODE" : "PO_NUMBER";
            foreach (IntegrationImportRowResponseDto row in normalized.Where(item => item.IsValid))
            {
                if (!seen.Add(row.Values[keyColumn] ?? string.Empty))
                {
                    row.IsValid = false;
                    row.Errors.Add("Duplicate key in this spreadsheet.");
                }
            }

            return normalized;
        }

        public byte[] BuildSpreadsheet(List<string> columns, List<Dictionary<string, string?>> rows)
        {
            using MemoryStream output = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                AddZipEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
                AddZipEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                AddZipEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Data\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
                AddZipEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
                StringBuilder sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
                AppendSpreadsheetRow(sheet, columns.Select(item => (string?)item).ToList(), 1);
                for (int index = 0; index < rows.Count; index++)
                {
                    Dictionary<string, string?> row = rows[index];
                    AppendSpreadsheetRow(sheet, columns.Select(column => row.TryGetValue(column, out string? value) ? value : null).ToList(), index + 2);
                }

                sheet.Append("</sheetData></worksheet>");
                AddZipEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
            }

            return output.ToArray();
        }

        public byte[] BuildCorrectionReport(IntegrationImportKind kind, List<IntegrationImportRowResponseDto> rows)
        {
            List<string> columns = Columns(kind);
            StringBuilder csv = new StringBuilder();
            csv.Append('﻿');
            AppendCsvRow(csv, new[] { "Row number" }.Concat(columns).Append("Validation errors"));
            foreach (IntegrationImportRowResponseDto row in rows.Where(item => !item.IsValid))
            {
                IEnumerable<string?> values = columns.Select(column => row.Values.TryGetValue(column, out string? value) ? value : null);
                AppendCsvRow(csv, new string?[] { row.RowNumber.ToString(CultureInfo.InvariantCulture) }.Concat(values).Append(string.Join(" | ", row.Errors)));
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public List<ApiFieldMapping> ImportMappings(IntegrationImportKind kind)
        {
            string prefix = kind == IntegrationImportKind.SUPPLIERS ? "Supplier" : "PurchaseOrder";
            return Columns(kind).Select(column => new ApiFieldMapping
            {
                Id = Guid.NewGuid(),
                SourceField = $"{prefix}.{ToPascal(column)}",
                TargetField = $"{prefix}.{ToPascal(column)}",
                NullPolicy = IntegrationNullPolicy.IGNORE_NULL,
                IsValidated = true
            }).ToList();
        }

        public JsonElement ToCanonicalRecord(IntegrationImportKind kind, Dictionary<string, string?> values)
        {
            string target = kind == IntegrationImportKind.SUPPLIERS ? "Supplier" : "PurchaseOrder";
            Dictionary<string, string?> fields = new Dictionary<string, string?>();
            foreach (string column in Columns(kind))
            {
                fields[ToPascal(column)] = values.TryGetValue(column, out string? value) ? value : null;
            }

            using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object?> { [target] = fields }));
            return document.RootElement.Clone();
        }

        private Dictionary<string, string?> NormalizeRow(IntegrationImportKind kind, Dictionary<string, string?> source, out List<string> errors)
        {
            errors = new List<string>();
            Dictionary<string, string?> values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (string column in Columns(kind))
            {
                KeyValuePair<string, string?> match = source.FirstOrDefault(item => NormalizeHeader(item.Key) == NormalizeHeader(column));
                string? value = string.IsNullOrWhiteSpace(match.Key) ? null : match.Value?.Trim();
                if (column is "SUPPLIER_CODE" or "PO_NUMBER" or "CURRENCY")
                {
                    value = value?.ToUpperInvariant();
                }

                values[column] = string.IsNullOrWhiteSpace(value) ? null : value;
            }

            if (kind == IntegrationImportKind.SUPPLIERS)
            {
                Require(values, "SUPPLIER_CODE", "Supplier code is required.", errors);
                Require(values, "NAME", "Supplier name is required.", errors);
                RequireCurrency(values, errors);
            }
            else
            {
                Require(values, "PO_NUMBER", "PO number is required.", errors);
                Require(values, "SUPPLIER_CODE", "Supplier code is required.", errors);
                Require(values, "SUPPLIER_NAME", "Supplier name is required.", errors);
                RequireCurrency(values, errors);
                Require(values, "COMPANY_CODE", "Company code is required.", errors);
                Require(values, "PURCHASE_ORDER_TYPE", "Purchase order type is required.", errors);
                Require(values, "PO_DATE", "PO date is required.", errors);
                Require(values, "DELIVERY_DATE", "Delivery date is required.", errors);
                foreach (string amountColumn in new[] { "TOTAL_NET_AMOUNT", "TOTAL_TAX_AMOUNT", "TOTAL_AMOUNT" })
                {
                    if (!decimal.TryParse(values[amountColumn], NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        errors.Add($"{amountColumn} is required and must be a valid decimal.");
                    }
                }
            }

            foreach (string dateColumn in new[] { "PO_DATE", "DELIVERY_DATE", "SOURCE_LAST_CHANGED_AT" })
            {
                if (!values.TryGetValue(dateColumn, out string? date) || string.IsNullOrWhiteSpace(date))
                {
                    continue;
                }

                bool valid = dateColumn == "SOURCE_LAST_CHANGED_AT"
                    ? DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _)
                    : DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
                if (!valid)
                {
                    errors.Add($"{dateColumn} is not a valid date.");
                }
            }

            return values;
        }

        private static void Require(Dictionary<string, string?> values, string column, string message, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(values[column]))
            {
                errors.Add(message);
            }
        }

        private static void RequireCurrency(Dictionary<string, string?> values, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(values["CURRENCY"]))
            {
                errors.Add("Currency is required.");
            }
            else if (!Regex.IsMatch(values["CURRENCY"]!, "^[A-Z]{3}$"))
            {
                errors.Add("Currency must be a three-letter ISO code.");
            }
        }

        private static string NormalizeHeader(string value)
        {
            return Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]", string.Empty);
        }

        private static string ToPascal(string value)
        {
            return string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
        }

        private static List<Dictionary<string, string?>> ParseXlsx(byte[] bytes)
        {
            try
            {
                using ZipArchive archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
                ZipArchiveEntry sheet = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException();
                ZipArchiveEntry? sharedEntry = archive.GetEntry("xl/sharedStrings.xml");
                List<string> shared = new List<string>();
                if (sharedEntry != null)
                {
                    using Stream sharedStream = sharedEntry.Open();
                    shared = XDocument.Load(sharedStream).Descendants().Where(item => item.Name.LocalName == "si")
                        .Select(item => string.Concat(item.Descendants().Where(text => text.Name.LocalName == "t").Select(text => text.Value)))
                        .ToList();
                }

                using Stream sheetStream = sheet.Open();
                List<Dictionary<int, string>> rows = XDocument.Load(sheetStream).Descendants().Where(item => item.Name.LocalName == "row").Select(row =>
                {
                    Dictionary<int, string> cells = new Dictionary<int, string>();
                    int position = 0;
                    foreach (XElement cell in row.Elements().Where(item => item.Name.LocalName == "c"))
                    {
                        // Excel leaves empty cells out of the row: the column comes from the cell reference (A1, C1, ...).
                        int column = ColumnIndex(cell.Attribute("r")?.Value) ?? position;
                        string? type = cell.Attribute("t")?.Value;
                        string? raw = cell.Elements().FirstOrDefault(item => item.Name.LocalName == "v")?.Value;
                        cells[column] = type == "inlineStr"
                            ? string.Concat(cell.Descendants().Where(item => item.Name.LocalName == "t").Select(item => item.Value))
                            : type == "s" && int.TryParse(raw, out int index) && index < shared.Count
                                ? shared[index]
                                : raw ?? string.Empty;
                        position = column + 1;
                    }

                    return cells;
                }).ToList();
                if (rows.Count == 0)
                {
                    return new List<Dictionary<string, string?>>();
                }

                Dictionary<int, string> headers = rows[0];
                return rows.Skip(1).Where(row => row.Values.Any(value => !string.IsNullOrWhiteSpace(value))).Select(row =>
                {
                    Dictionary<string, string?> result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (KeyValuePair<int, string> header in headers.Where(item => !string.IsNullOrWhiteSpace(item.Value)))
                    {
                        result[header.Value] = row.TryGetValue(header.Key, out string? value) ? value : null;
                    }

                    return result;
                }).ToList();
            }
            catch (Exception exception) when (exception is InvalidDataException or InvalidOperationException or XmlException or FormatException)
            {
                throw new IntegrationException("IMPORT_FORMAT_INVALID", "The upload is not a valid .xlsx spreadsheet.");
            }
        }

        private static int? ColumnIndex(string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }

            int index = 0;
            foreach (char character in reference.TakeWhile(char.IsLetter))
            {
                index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
            }

            return index == 0 ? null : index - 1;
        }

        private static List<Dictionary<string, string?>> ParseCsv(string value)
        {
            List<List<string>> records = new List<List<string>>();
            List<string> current = new List<string>();
            StringBuilder cell = new StringBuilder();
            bool quoted = false;
            value = value.TrimStart('﻿');
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character == '"' && quoted && index + 1 < value.Length && value[index + 1] == '"')
                {
                    cell.Append('"');
                    index++;
                    continue;
                }

                if (character == '"')
                {
                    quoted = !quoted;
                    continue;
                }

                if (character == ',' && !quoted)
                {
                    current.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }

                if ((character == '\n' || character == '\r') && !quoted)
                {
                    if (character == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
                    {
                        index++;
                    }

                    current.Add(cell.ToString());
                    cell.Clear();
                    if (current.Any(item => !string.IsNullOrWhiteSpace(item)))
                    {
                        records.Add(current);
                    }

                    current = new List<string>();
                    continue;
                }

                cell.Append(character);
            }

            if (cell.Length > 0 || current.Count > 0)
            {
                current.Add(cell.ToString());
                records.Add(current);
            }

            if (records.Count == 0)
            {
                return new List<Dictionary<string, string?>>();
            }

            List<string> headers = records[0];
            return records.Skip(1).Where(row => row.Any(item => !string.IsNullOrWhiteSpace(item))).Select(row =>
            {
                Dictionary<string, string?> result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < headers.Count; index++)
                {
                    result[headers[index]] = index < row.Count ? row[index] : null;
                }

                return result;
            }).ToList();
        }

        private static void AppendSpreadsheetRow(StringBuilder sheet, List<string?> values, int rowNumber)
        {
            sheet.Append($"<row r=\"{rowNumber}\">");
            for (int index = 0; index < values.Count; index++)
            {
                string? value = SecurityElement.Escape(values[index] ?? string.Empty);
                sheet.Append($"<c r=\"{ColumnName(index + 1)}{rowNumber}\" t=\"inlineStr\"><is><t>{value}</t></is></c>");
            }

            sheet.Append("</row>");
        }

        private static void AppendCsvRow(StringBuilder csv, IEnumerable<string?> values)
        {
            csv.AppendLine(string.Join(',', values.Select(value => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"")));
        }

        private static string ColumnName(int number)
        {
            string result = string.Empty;
            while (number > 0)
            {
                int remainder = (number - 1) % 26;
                result = (char)('A' + remainder) + result;
                number = (number - 1) / 26;
            }

            return result;
        }

        private static void AddZipEntry(ZipArchive archive, string name, string content)
        {
            using StreamWriter writer = new StreamWriter(archive.CreateEntry(name, CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
            writer.Write(content);
        }
    }
}
