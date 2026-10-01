namespace Operations.Domain.Dtos
{
    public class InvoiceExtractionLineResponseDto
    {
        public string? ItemSkuId { get; set; }
        public decimal? ItemAmount { get; set; }
        public decimal? ItemNet { get; set; }
        public string? ItemDescription { get; set; }
        public string? LineItemNumber { get; set; }
        public Guid? PurchaseOrderItemId { get; set; }
    }
}
