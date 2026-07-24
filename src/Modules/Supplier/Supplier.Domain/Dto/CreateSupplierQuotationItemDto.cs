namespace Supplier.Domain.Dto
{public class CreateSupplierQuotationItemDto
{
    public Guid SupplierRFQItemId { get; set; }

    public Guid BuyerRFQItemId { get; set; }

    public decimal QuotedPrice { get; set; }
}
}