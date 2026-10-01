using Microsoft.AspNetCore.Http;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    /// <summary>
    /// Multipart form of POST documents/invoices. The organization is never part of the request.
    /// </summary>
    public class UploadInvoiceRequestDto
    {
        public Guid? OperatingUnitId { get; set; }
        public DocumentSourceChannel SourceChannel { get; set; }
        public int? PageCount { get; set; }
        public string? ScanSessionId { get; set; }
        public string? SupplierName { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierTrn { get; set; }
        public string? SupplierInvoiceNumber { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public bool NoPurchaseOrder { get; set; }
        public decimal? InvoiceGross { get; set; }
        public string? Currency { get; set; }
        public string? OcrRequestId { get; set; }
        public string? ManualEditedFieldsJson { get; set; }
        public bool DeferFullExtraction { get; set; }
        public IFormFile? File { get; set; }
    }
}
