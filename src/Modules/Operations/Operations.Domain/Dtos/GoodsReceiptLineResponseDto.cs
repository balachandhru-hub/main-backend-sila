namespace Operations.Domain.Dtos
{
    public class GoodsReceiptLineResponseDto
    {
        public Guid Id { get; set; }
        public Guid PurchaseOrderItemId { get; set; }
        public int PurchaseOrderLineNumber { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OpenQuantityBefore { get; set; }
        public decimal? InvoiceQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal AcceptedQuantity { get; set; }
        public decimal DamagedQuantity { get; set; }
        public decimal RejectedQuantity { get; set; }
        public string Uom { get; set; } = string.Empty;
        public string? BatchNumber { get; set; }
        public DateOnly? ExpiryDate { get; set; }
    }
}
