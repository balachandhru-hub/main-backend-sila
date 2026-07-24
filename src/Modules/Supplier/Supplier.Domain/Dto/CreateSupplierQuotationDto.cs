namespace Supplier.Domain.Dto
{
    public class CreateSupplierQuotationDto
{
    public Guid SupplierRFQId { get; set; }

    public Guid BuyerRFQId { get; set; }

    public string RFQNumber { get; set; }

    public Guid BuyerId { get; set; }

    public Guid SupplierId { get; set; }

    public decimal? DeliveryCharge { get; set; }

    public decimal? Tax { get; set; }

    public decimal? Discount { get; set; }

    public string? DeliveryType { get; set; }

    public decimal? TotalPrice { get; set; }

    public string? TaxType { get; set; }

   

    public string? DiscountType { get; set; }

    public List<CreateSupplierQuotationItemDto> Items { get; set; }
}
}