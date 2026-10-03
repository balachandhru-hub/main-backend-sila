using System.Globalization;
using System.Text;
using SilaMe.Api.Services;

namespace SilaMe.Api.Services;

public static class StockCountReportFiles
{
    public static byte[] Excel(StockShortageReport report)
    {
        var xml = new StringBuilder();
        xml.Append("""<?xml version="1.0"?><?mso-application progid="Excel.Sheet"?><Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet"><Worksheet ss:Name="Summary"><Table>""");
        Row(xml, "Reporting period", Period(report));
        Row(xml, "Locations", report.Locations.ToString(CultureInfo.InvariantCulture));
        Row(xml, "Shortage items", report.Materials.ToString(CultureInfo.InvariantCulture));
        Row(xml, "Shortage quantity", report.ShortageQuantity is null ? "Mixed units" : $"{report.ShortageQuantity} {report.QuantityUom}");
        Row(xml, "Total shortage value", Money(report.TotalValue, report.Currency));
        Row(xml, "Awaiting justification", Money(report.AwaitingValue, report.Currency));
        Row(xml, "Justified", Money(report.JustifiedValue, report.Currency));
        Row(xml, "Unresolved", Money(report.UnresolvedValue, report.Currency));
        Row(xml, "Approved adjustment", Money(report.ApprovedValue, report.Currency));
        Row(xml, "SAP posted", Money(report.PostedValue, report.Currency));
        Row(xml, "SAP failed or pending", Money(report.FailedValue, report.Currency));
        xml.Append("</Table></Worksheet><Worksheet ss:Name=\"Reasons\"><Table>");
        Row(xml, "Reason", "Count", "Value", "Percent");
        foreach (var reason in report.ByReason) Row(xml, reason.Category, reason.Count.ToString(CultureInfo.InvariantCulture), Money(reason.Value, report.Currency), reason.Percent?.ToString(CultureInfo.InvariantCulture));
        xml.Append("</Table></Worksheet><Worksheet ss:Name=\"Detail\"><Table>");
        Row(xml, "Property", "Location", "Manager", "Count", "Date", "Material", "Description", "Group", "System", "Physical", "Shortage", "UOM", "Unit cost", "Value", "Currency", "Reason", "Comments", "Response", "Reviewer", "Decision", "SAP", "Document", "Enquiry");
        foreach (var line in report.Lines)
            Row(xml, line.Property, line.Location, line.Manager ?? line.ManagerGroup, line.CountNumber, line.CountDate.ToString("yyyy-MM-dd"), line.MaterialCode, line.Description, line.MaterialGroup, line.SystemQty.ToString(CultureInfo.InvariantCulture), line.PhysicalQty?.ToString(CultureInfo.InvariantCulture), line.ShortageQty.ToString(CultureInfo.InvariantCulture), line.Uom, line.UnitCost?.ToString(CultureInfo.InvariantCulture), line.ShortageValue?.ToString(CultureInfo.InvariantCulture), line.Currency, line.Category, line.Comments, line.ResponseAt?.ToString("yyyy-MM-dd"), line.Reviewer, line.ReviewDecision, line.SapStatus, line.SapMaterialDocument, line.EnquiryNumber);
        xml.Append("</Table></Worksheet></Workbook>");
        return Encoding.UTF8.GetBytes(xml.ToString());
    }

    public static byte[] Pdf(StockShortageReport report)
    {
        var lines = new List<string>
        {
            "SILA Inventory Shortage Report",
            Period(report),
            $"Locations {report.Locations}   Shortage items {report.Materials}",
            $"Quantity {(report.ShortageQuantity is null ? "not aggregated across units" : report.ShortageQuantity + " " + report.QuantityUom)}",
            $"Total {Money(report.TotalValue, report.Currency)}   Awaiting {Money(report.AwaitingValue, report.Currency)}   Justified {Money(report.JustifiedValue, report.Currency)}",
            $"Unresolved {Money(report.UnresolvedValue, report.Currency)}   Approved {Money(report.ApprovedValue, report.Currency)}   SAP posted {Money(report.PostedValue, report.Currency)}   SAP open {Money(report.FailedValue, report.Currency)}",
            "",
        };
        foreach (var reason in report.ByReason) lines.Add($"{reason.Category}: {reason.Count}  {Money(reason.Value, report.Currency)}  {reason.Percent}%");
        lines.Add("");
        foreach (var line in report.Lines.Take(80))
            lines.Add($"{line.CountDate:yyyy-MM-dd} {line.Location} {line.MaterialCode} {Trim(line.Description, 28)} short {line.ShortageQty} {line.Uom} {Money(line.ShortageValue, line.Currency)} {line.Category} {line.SapStatus}");
        if (report.Lines.Count > 80) lines.Add($"… {report.Lines.Count - 80} more lines are in the Excel file.");
        return SimplePdf(lines);
    }

    private static void Row(StringBuilder xml, params string?[] cells)
    {
        xml.Append("<Row>");
        foreach (var cell in cells) xml.Append("<Cell><Data ss:Type=\"String\">").Append(Escape(cell)).Append("</Data></Cell>");
        xml.Append("</Row>");
    }

    private static string Escape(string? value) => System.Security.SecurityElement.Escape(value ?? "") ?? "";
    private static string Period(StockShortageReport report) => $"{report.From:yyyy-MM-dd} to {report.To:yyyy-MM-dd}";
    private static string Money(decimal? value, string? currency) => value is null ? "—" : $"{value.Value.ToString("0.00", CultureInfo.InvariantCulture)} {currency}".Trim();
    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max];

    private static byte[] SimplePdf(IReadOnlyList<string> lines)
    {
        var commands = new StringBuilder("BT /F1 9 Tf 40 800 Td 12 TL ");
        foreach (var line in lines)
            commands.Append('(').Append(PdfText(line)).Append(") ' ");
        commands.Append("ET");
        var stream = commands.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {stream.Length} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(index + 1).Append(" 0 obj\n").Append(objects[index]).Append("\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) pdf.Append(offset.ToString("D10")).Append(" 00000 n \n");
        pdf.Append("trailer << /Size ").Append(objects.Length + 1).Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string PdfText(string value)
    {
        var cleaned = new string(value.Select(item => item is >= ' ' and <= '~' ? item : ' ').ToArray());
        return cleaned.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
