namespace Buyer.Domain.Dto
{
    public class QuotationAuditRequestDto
    {
       public Guid BuyerRFQId { get; set; }

       public Guid SupplierQuotationId { get; set; }

        public Guid SupplierId { get; set; }

        public Guid BuyerId { get; set; }

        public string RFQNumber { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public string? DeliveryType { get; set; }

        public decimal? Discount { get; set; }

        public string? DiscountType { get; set; }

        public decimal? Tax { get; set; }

        public string? TaxType { get; set; }

        public List<QuotationAuditItemDto> Items { get; set; } = new();
    }

    public class QuotationAuditItemDto
    {
        public Guid SupplierQuotationItemId { get; set; }

        public Guid SupplierRfqItemId { get; set; }

        public Guid BuyerRfqItemId { get; set; }

        public decimal QuotedPrice { get; set; }
    }
}