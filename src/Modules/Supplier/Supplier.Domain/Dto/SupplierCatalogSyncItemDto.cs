namespace Supplier.Domain.Dto
{
    /// <summary>One product (or the stock of one product) read from the supplier's own API.</summary>
    public class SupplierCatalogSyncItemDto
    {
        public string Sku { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal? DiscountPercent { get; set; }
    }
}
