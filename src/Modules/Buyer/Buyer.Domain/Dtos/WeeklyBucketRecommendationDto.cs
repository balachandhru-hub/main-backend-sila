namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketRecommendationDto
    {
        public Guid Id { get; set; }
        public string RecommendationNumber { get; set; } = string.Empty;
        public Guid WeeklyBucketItemId { get; set; }
        public Guid CatalogId { get; set; }
        public string? Sku { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? DecidedBy { get; set; }
        public string? DecidedByName { get; set; }
        public DateTime? DecidedOn { get; set; }
    }
}
