using System.Globalization;
using System.Text.RegularExpressions;

namespace Operations.Application.Services.Ocr
{
    /// <summary>
    /// Reads the invoice header and "LINE n | ..." rows from the text of a document.
    /// </summary>
    public static class InvoiceTextParser
    {
        public static ExtractedInvoice Parse(string text)
        {
            List<ExtractedInvoiceLine> lines = new List<ExtractedInvoiceLine>();
            foreach (Match match in Regex.Matches(text, @"LINE\s+(?<line>\d+)\s*\|\s*(?:(?<sku>[^|]+)\s*\|\s*)?(?<description>[^|]+)\s*\|\s*(?:(?<qty>[\d.,]+)\s*(?<uom>[A-Za-z]+)\s*\|\s*)?(?<amount>[\d.,]+)(?:\s*\|\s*(?<net>[\d.,]+))?", RegexOptions.IgnoreCase))
            {
                bool hasQuantity = decimal.TryParse(match.Groups["qty"].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quantity);
                decimal? amount = decimal.TryParse(match.Groups["amount"].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmount) ? parsedAmount : null;
                decimal? net = decimal.TryParse(match.Groups["net"].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedNet) ? parsedNet : amount;
                lines.Add(new ExtractedInvoiceLine
                {
                    LineNumber = int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture),
                    SupplierMaterialCode = match.Groups["sku"].Success ? match.Groups["sku"].Value.Trim() : null,
                    Description = match.Groups["description"].Value.Trim(),
                    Quantity = hasQuantity ? quantity : null,
                    Uom = match.Groups["uom"].Success ? match.Groups["uom"].Value.Trim().ToUpperInvariant() : null,
                    UnitPrice = hasQuantity && amount.HasValue && quantity != 0 ? amount / quantity : null,
                    LineAmount = amount ?? net
                });
            }

            return new ExtractedInvoice
            {
                InvoiceNumber = LabelValue(text, "Invoice Number", "Invoice No", "Invoice #", "Tax Invoice No", "Tax Invoice Number", "Inv No", "Document No"),
                InvoiceDate = Date(text, @"(?:Invoice Date|Tax Invoice Date|Document Date|Date)\s*(?:[:#\-]|\s)\s*(?<value>\d{1,4}[./-]\d{1,2}[./-]\d{1,4})"),
                SupplierName = LabelValue(text, "Supplier", "Supplier Name", "Vendor", "Vendor Name"),
                SupplierTaxNumber = LabelValue(text, "TRN", "VAT TRN", "Tax Registration Number", "VAT Registration Number", "Tax Number", "Tax No"),
                PoNumber = LabelValue(text, "PO Number", "PO No", "Purchase Order", "Purchase Order Number", "Customer PO", "Order Ref"),
                Currency = LabelValue(text, "Currency"),
                NetAmount = Number(text, @"(?:Net Amount|Net Total|Subtotal)\s*(?:[:#\-]|\s)\s*(?<value>[\d.,]+)"),
                TaxAmount = Number(text, @"(?:Tax Amount|VAT Amount|VAT)\s*(?:[:#\-]|\s)\s*(?<value>[\d.,]+)"),
                GrossAmount = Number(text, @"(?:Grand Total|Gross Total|Invoice Total|Total Amount|Amount Due|Total Including VAT|Total Incl VAT|Gross Amount)\s*(?:[:#\-]|\s)\s*(?<value>[\d.,]+)"),
                Lines = lines
            };
        }

        private static string? LabelValue(string text, params string[] labels)
        {
            string pattern = string.Join("|", labels.OrderByDescending(label => label.Length).Select(Regex.Escape));
            Match match = Regex.Match(text, $@"(?:{pattern})\s*(?:[:#\-]|\s)\s*(?<value>[^\r\n|]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value.Trim() : null;
        }

        private static decimal? Number(string text, string pattern)
        {
            string raw = Regex.Match(text, pattern, RegexOptions.IgnoreCase).Groups["value"].Value.Replace(",", string.Empty).Trim();
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value : null;
        }

        private static DateOnly? Date(string text, string pattern)
        {
            string raw = Regex.Match(text, pattern, RegexOptions.IgnoreCase).Groups["value"].Value.Trim();
            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly value))
            {
                return value;
            }

            return DateOnly.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out value) ? value : null;
        }
    }
}
