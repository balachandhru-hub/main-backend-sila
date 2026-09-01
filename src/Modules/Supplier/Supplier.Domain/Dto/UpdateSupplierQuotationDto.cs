namespace Supplier.Domain.Dto
{
    public class UpdateSupplierQuotationDto
    {
        public Guid SupplierQuotationId { get; set; }

        public decimal? TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }
        public string? DeliveryType { get; set; }

        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; }

        public decimal? Tax { get; set; }
        public string? TaxType { get; set; }

        public List<CreateSupplierQuotationItemDto>? Items { get; set; }
        public string? TemporaryVerificationToken { get; set; }
    }
}