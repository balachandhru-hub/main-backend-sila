using Operations.Domain.Enums;

namespace Operations.Application.Services.Ocr
{
    /// <summary>Invoice fields read from the text of a document.</summary>
    public class ExtractedInvoice
    {
        public string? InvoiceNumber { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierTaxNumber { get; set; }
        public string? PoNumber { get; set; }
        public string? Currency { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? GrossAmount { get; set; }
        public List<ExtractedInvoiceLine> Lines { get; set; } = new();
    }

    public class ExtractedInvoiceLine
    {
        public int LineNumber { get; set; }
        public string? SupplierMaterialCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public string? Uom { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? LineAmount { get; set; }
    }

    /// <summary>Text of a document and how it was obtained.</summary>
    public class OcrTextResult
    {
        public string? Text { get; set; }
        public decimal? Confidence { get; set; }
        public string Provider { get; set; } = string.Empty;
        public ExtractionMethod Method { get; set; }
        public bool FallbackUsed { get; set; }
        public string? ErrorCategory { get; set; }

        /// <summary>
        /// True when nothing was read because the OCR binaries (pdftoppm / tesseract) are not
        /// installed. The document is kept and the invoice is handed to manual entry.
        /// </summary>
        public bool OcrUnavailable { get; set; }
    }
}
