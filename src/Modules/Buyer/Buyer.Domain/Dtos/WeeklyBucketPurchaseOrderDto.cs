namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketPurchaseOrderDto
    {
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? DocumentNumber { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
