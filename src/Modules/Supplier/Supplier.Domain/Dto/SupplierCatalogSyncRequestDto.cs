namespace Supplier.Domain.Dto
{
    public class SupplierCatalogSyncRequestDto
    {
        public const string MODE_CATALOG = "CATALOG";
        public const string MODE_STOCK = "STOCK";

        /// <summary>The supplier organization the products belong to.</summary>
        public Guid OrganizationId { get; set; }

        /// <summary>CATALOG creates and updates products; STOCK only updates the stock of existing ones.</summary>
        public string Mode { get; set; } = MODE_CATALOG;
        public List<SupplierCatalogSyncItemDto> Items { get; set; } = new();
    }
}
