namespace Operations.Domain.Dtos
{
    public class InvoiceExtractionHeaderResponseDto
    {
        public Guid DocumentId { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierTrn { get; set; }
        public string? SupplierInvoiceNumber { get; set; }
        public DateOnly? InvoiceDate { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public decimal? InvoiceGross { get; set; }
        public decimal? InvoiceNet { get; set; }
        public string? Currency { get; set; }
    }
}
