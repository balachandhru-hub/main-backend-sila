using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class GoodsReceiptResponseDto
    {
        public Guid Id { get; set; }
        public string GrnNumber { get; set; } = string.Empty;
        public GoodsReceiptStatus Status { get; set; }
        public Guid PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public Guid? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public Guid OrganizationId { get; set; }
        public Guid OperatingUnitId { get; set; }
        public string OperatingUnitName { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PostedAt { get; set; }
        public string? BusinessStatus { get; set; }
        public string? ErpPostingStatus { get; set; }
        public string? ErpMaterialDocument { get; set; }
        public string? ErpDocumentYear { get; set; }
        public string? ErpResponseJson { get; set; }
        public string? FailureCode { get; set; }
        public string? FailureMessage { get; set; }
        public int ErpAttemptCount { get; set; }
        public List<GoodsReceiptLineResponseDto> Lines { get; set; } = new();
    }
}
