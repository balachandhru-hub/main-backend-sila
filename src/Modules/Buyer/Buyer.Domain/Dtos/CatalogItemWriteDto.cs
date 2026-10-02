namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// A catalog product and a quantity. Name, supplier, unit and price are read from the Supplier service.
    /// </summary>
    public class CatalogItemWriteDto
    {
        public Guid CatalogId { get; set; }
        public decimal Quantity { get; set; }
    }
}
