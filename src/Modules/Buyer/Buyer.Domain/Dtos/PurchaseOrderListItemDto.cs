namespace Buyer.Domain.Dtos
{
    public class PurchaseOrderListItemDto
    {
        public Guid Id { get; set; }
        public string PoNumber { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? BuyerName { get; set; }
        public string? BucketCode { get; set; }
        public string? PlantCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Currency { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public int ItemCount { get; set; }
    }
}
