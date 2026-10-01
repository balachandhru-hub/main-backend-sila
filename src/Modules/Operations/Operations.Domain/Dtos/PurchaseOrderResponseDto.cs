using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class PurchaseOrderResponseDto
    {
        public Guid Id { get; set; }
        public string PoNumber { get; set; } = string.Empty;
        public DateOnly? PoDate { get; set; }
        public DateOnly? DeliveryDate { get; set; }
        public string Currency { get; set; } = string.Empty;
        public PurchaseOrderStatus Status { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? OperatingUnitId { get; set; }
        public string? OperatingUnitName { get; set; }
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public List<PurchaseOrderItemResponseDto> Items { get; set; } = new();
        public string EntityCode { get; set; } = "DEFAULT";
        public string? PurchaseOrderType { get; set; }
        public string? CompanyCode { get; set; }
        public string? ErpSupplierId { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? PaymentTerms { get; set; }
        public string? PoCategory { get; set; }
        public decimal? TotalNetAmount { get; set; }
        public decimal? TotalTaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? TotalOrderedQuantity { get; set; }
        public decimal? TotalReceivedQuantity { get; set; }
        public string? SourceSystem { get; set; }
        public DateTime? SourceLastChangedAt { get; set; }
        public DateTime? LastSyncedAt { get; set; }
    }
}
