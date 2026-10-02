using System.IO.Compression;
using System.Xml.Linq;

namespace SilaMe.Api.Services;

internal static class ExcelOpenXml
{
    internal sealed record Sheet(string Name, IReadOnlyList<IReadOnlyList<string>> Rows);

    internal static IReadOnlyList<Sheet> ReadSheets(Stream file)
    {
        using var archive = new ZipArchive(file, ZipArchiveMode.Read, true);
        if (archive.GetEntry("xl/workbook.xml") is null) throw new InvalidDataException();
        var shared = ReadSharedStrings(archive);
        var sheets = new List<Sheet>();
        foreach (var (name, path) in SheetTargets(archive))
        {
            var entry = archive.GetEntry(path) ?? archive.GetEntry(path.Replace("xl/", "xl/worksheets/", StringComparison.OrdinalIgnoreCase));
            if (entry is null) continue;
            using var stream = entry.Open();
            var rows = XDocument.Load(stream).Descendants().Where(item => item.Name.LocalName == "row")
                .Select(row => (IReadOnlyList<string>)ReadRow(row, shared))
                .ToList();
            sheets.Add(new Sheet(name, rows));
        }

        if (sheets.Count == 0)
        {
            var fallback = archive.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException();
            using var stream = fallback.Open();
            var rows = XDocument.Load(stream).Descendants().Where(item => item.Name.LocalName == "row")
                .Select(row => (IReadOnlyList<string>)ReadRow(row, shared))
                .ToList();
            sheets.Add(new Sheet("Data", rows));
        }

        return sheets;
    }

    internal static Sheet? FindSheet(IReadOnlyList<Sheet> sheets, params string[] names)
    {
        foreach (var name in names)
        {
            var match = sheets.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }
        return null;
    }

    internal static IReadOnlyList<Dictionary<string, string?>> ToDictionaries(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows.Count == 0) return [];
        var headers = rows[0].ToList();
        return rows.Skip(1).Where(row => row.Any(value => !string.IsNullOrWhiteSpace(value))).Select(row =>
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(headers[index])) continue;
                values[headers[index]] = index < row.Count ? EmptyToNull(row[index]) : null;
            }
            return values;
        }).ToList();
    }

    private static IReadOnlyList<(string Name, string Path)> SheetTargets(ZipArchive archive)
    {
        using var workbookStream = archive.GetEntry("xl/workbook.xml")!.Open();
        var workbook = XDocument.Load(workbookStream);
        var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (archive.GetEntry("xl/_rels/workbook.xml.rels") is { } relsEntry)
        {
            using var relsStream = relsEntry.Open();
            foreach (var relationship in XDocument.Load(relsStream).Descendants().Where(item => item.Name.LocalName == "Relationship"))
            {
                var id = relationship.Attribute("Id")?.Value;
                var target = relationship.Attribute("Target")?.Value;
                if (id is null || target is null) continue;
                var path = target.Replace('\\', '/');
                if (!path.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) path = "xl/" + path.TrimStart('/');
                if (path.StartsWith("xl/xl/", StringComparison.OrdinalIgnoreCase)) path = path[3..];
                targets[id] = path;
            }
        }

        var sheets = new List<(string Name, string Path)>();
        var index = 0;
        foreach (var sheet in workbook.Descendants().Where(item => item.Name.LocalName == "sheet"))
        {
            index++;
            var name = sheet.Attribute("name")?.Value ?? $"Sheet{index}";
            var relId = sheet.Attributes().FirstOrDefault(item => item.Name.LocalName == "id")?.Value;
            var path = relId is not null && targets.TryGetValue(relId, out var target)
                ? target
                : $"xl/worksheets/sheet{index}.xml";
            sheets.Add((name, path));
        }
        return sheets;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        if (archive.GetEntry("xl/sharedStrings.xml") is not { } entry) return [];
        using var stream = entry.Open();
        return XDocument.Load(stream).Descendants().Where(item => item.Name.LocalName == "si")
            .Select(item => string.Concat(item.Descendants().Where(text => text.Name.LocalName == "t").Select(text => text.Value)))
            .ToList();
    }

    private static List<string> ReadRow(XElement row, IReadOnlyList<string> shared)
    {
        var values = new List<string>();
        var next = 0;
        foreach (var cell in row.Elements().Where(item => item.Name.LocalName == "c"))
        {
            var index = ColumnIndex(cell.Attribute("r")?.Value, next);
            while (values.Count <= index) values.Add(string.Empty);
            values[index] = CellText(cell, shared);
            next = index + 1;
        }
        return values;
    }

    private static string CellText(XElement cell, IReadOnlyList<string> shared)
    {
        var type = cell.Attribute("t")?.Value;
        if (type is "inlineStr" or "str")
            return string.Concat(cell.Descendants().Where(item => item.Name.LocalName == "t").Select(item => item.Value));
        var raw = cell.Elements().FirstOrDefault(item => item.Name.LocalName == "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(raw, out var index) && index >= 0 && index < shared.Count)
            return shared[index];
        return raw;
    }

    private static int ColumnIndex(string? reference, int fallback)
    {
        if (string.IsNullOrWhiteSpace(reference)) return fallback;
        var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
        if (letters.Length == 0) return fallback;
        var index = 0;
        foreach (var ch in letters)
            index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        return index - 1;
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    internal static byte[] Write(IReadOnlyList<string> columns, IReadOnlyList<Dictionary<string, string?>> rows) =>
        WriteSheets([new SheetWrite("Data", columns, rows)]);

    internal sealed record SheetWrite(string Name, IReadOnlyList<string> Columns, IReadOnlyList<Dictionary<string, string?>> Rows);

    internal static byte[] WriteSheets(IReadOnlyList<SheetWrite> sheets)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            void Add(string path, string content)
            {
                using var stream = archive.CreateEntry(path).Open();
                using var writer = new StreamWriter(stream);
                writer.Write(content);
            }

            var overrides = string.Concat(sheets.Select((_, index) =>
                $"<Override PartName=\"/xl/worksheets/sheet{index + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"));
            Add("[Content_Types].xml", $"""
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>{overrides}</Types>
                """);
            Add("_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            var workbookSheets = string.Concat(sheets.Select((sheet, index) =>
                $"<sheet name=\"{System.Security.SecurityElement.Escape(sheet.Name)}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>"));
            Add("xl/workbook.xml", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets>{workbookSheets}</sheets></workbook>""");
            var rels = string.Concat(sheets.Select((_, index) =>
                $"<Relationship Id=\"rId{index + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{index + 1}.xml\"/>"));
            Add("xl/_rels/workbook.xml.rels", $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{rels}</Relationships>""");
            for (var sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                var definition = sheets[sheetIndex];
                var xml = new System.Text.StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
                AppendRow(xml, definition.Columns.Select(item => (string?)item).ToList(), 1);
                for (var index = 0; index < definition.Rows.Count; index++)
                    AppendRow(xml, definition.Columns.Select(column => definition.Rows[index].TryGetValue(column, out var value) ? value : null).ToList(), index + 2);
                xml.Append("</sheetData></worksheet>");
                Add($"xl/worksheets/sheet{sheetIndex + 1}.xml", xml.ToString());
            }
        }
        return output.ToArray();
    }

    private static void AppendRow(System.Text.StringBuilder sheet, IReadOnlyList<string?> values, int rowNumber)
    {
        sheet.Append($"<row r=\"{rowNumber}\">");
        for (var index = 0; index < values.Count; index++)
        {
            var reference = $"{(char)('A' + index)}{rowNumber}";
            var text = System.Security.SecurityElement.Escape(values[index] ?? string.Empty) ?? string.Empty;
            sheet.Append($"<c r=\"{reference}\" t=\"inlineStr\"><is><t>{text}</t></is></c>");
        }
        sheet.Append("</row>");
    }
}
