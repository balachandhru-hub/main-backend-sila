namespace Supplier.Domain.Dto
{
    /// <summary>
    /// The stock details of a catalog product. Each value replaces the stored one; null clears it.
    /// </summary>
    public class UpdateSupplierCatalogStockDto
    {
        public string? Sku { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal? DiscountPercent { get; set; }
    }
}
