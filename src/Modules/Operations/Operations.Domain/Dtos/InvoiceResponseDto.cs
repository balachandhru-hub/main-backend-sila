using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class InvoiceResponseDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly? InvoiceDate { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierTaxNumber { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public bool NoPurchaseOrder { get; set; }
        public string? Currency { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? GrossAmount { get; set; }
        public InvoiceType InvoiceType { get; set; }
        public InvoiceStatus Status { get; set; }
        public decimal? OverallConfidence { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? OperatingUnitId { get; set; }
        public string? OperatingUnitName { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierCode { get; set; }
        public Guid? GoodsReceiptId { get; set; }
        public List<InvoiceLineResponseDto> Lines { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
