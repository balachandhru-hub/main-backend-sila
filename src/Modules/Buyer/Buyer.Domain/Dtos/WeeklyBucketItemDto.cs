namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketItemDto
    {
        public Guid Id { get; set; }
        public Guid CatalogId { get; set; }
        public string? Sku { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? OriginalProductName { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal ApprovedQuantity { get; set; }
        public decimal? SupplierStock { get; set; }
        public DateTime? SupplierStockRefreshedOn { get; set; }
        public decimal? StockInHand { get; set; }
        public string AvailabilityStatus { get; set; } = string.Empty;
        public string LineStatus { get; set; } = string.Empty;
        public Guid RequestorUserId { get; set; }
        public string? RequestorName { get; set; }
        public Guid OutletId { get; set; }
        public string? OutletName { get; set; }
        public string? StorageLocation { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
