namespace Supplier.Domain.Dto
{
    /// <summary>
    /// Current price and stock of one catalog product, as read by the Buyer service.
    /// </summary>
    public class BuyerCatalogStockDto
    {
        public Guid CatalogId { get; set; }

        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public string? Sku { get; set; }

        public string? CatalogName { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }

        public string? Currency { get; set; }

        public string? UnitOfMeasure { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal? AvailableStock { get; set; }
    }
}
