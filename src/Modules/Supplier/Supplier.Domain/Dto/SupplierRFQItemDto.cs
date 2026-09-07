
namespace Supplier.Domain.Dto{
public class SupplierRFQItemDto
{
    public Guid BuyerRFQItemId { get; set; }

    public string Description { get; set; }

    public decimal Quantity { get; set; }

    public string UOM { get; set; }

    public string? MaterialCode { get; set; }

    public string? MaterialGroup { get; set; }

    public string? CostCenter { get; set; }
    public int LineNumber { get; set; }
}
}