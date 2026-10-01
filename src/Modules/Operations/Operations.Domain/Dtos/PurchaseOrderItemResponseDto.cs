using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class PurchaseOrderItemResponseDto
    {
        public Guid Id { get; set; }
        public int LineNumber { get; set; }
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal OpenQuantity { get; set; }
        public string Uom { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public PurchaseOrderItemStatus Status { get; set; }
        public string? ItemNumber { get; set; }
        public decimal? PriceQuantity { get; set; }
        public decimal? ItemAmount { get; set; }
        public string? TaxCode { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? GrossItemAmount { get; set; }
        public string? Currency { get; set; }
        public string? MaterialGroup { get; set; }
        public string? Plant { get; set; }
        public string? StorageLocation { get; set; }
        public string? ItemCategory { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public bool GoodsReceiptExpected { get; set; } = true;
        public bool InvoiceExpected { get; set; } = true;
        public bool DeliveryCompleted { get; set; } = false;
        public bool DeletionIndicator { get; set; } = false;
    }
}
