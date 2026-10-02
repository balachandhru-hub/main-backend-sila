namespace Buyer.Domain.Dtos
{
    public class PersonalWishlistItemDto
    {
        public Guid Id { get; set; }
        public Guid CatalogId { get; set; }
        public string? Sku { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal Quantity { get; set; }
    }
}
