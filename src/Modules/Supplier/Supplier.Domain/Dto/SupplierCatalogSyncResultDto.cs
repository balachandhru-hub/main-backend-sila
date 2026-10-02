namespace Supplier.Domain.Dto
{
    public class SupplierCatalogSyncResultDto
    {
        public int Created { get; set; }
        public int Updated { get; set; }

        /// <summary>Items that matched no product (stock of a SKU the catalog does not have).</summary>
        public int Skipped { get; set; }
    }
}
