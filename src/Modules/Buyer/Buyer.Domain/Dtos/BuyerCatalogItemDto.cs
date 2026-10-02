namespace Buyer.Domain.Dto
{
    /// <summary>
    /// A product of the supplier product catalog, as returned by the Supplier service.
    /// </summary>
    public class BuyerCatalogItemDto
    {
        public Guid SupplierId { get; set; }
        public Guid? CatalogId { get; set; }
        public string? SupplierName { get; set; }
        public string? CatalogName { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string? Sku { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal? DiscountPercent { get; set; }
    }
}
