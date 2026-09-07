namespace Buyer.Domain.Dto
{
    public class GetAllSupplierQuotationDto
    {
        public List<SupplierQuotationBySupplierDto> Suppliers { get; set; } = new();
    }

    public class SupplierQuotationBySupplierDto
    {
        public Guid SupplierRFQId { get; set; }

        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public decimal? TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? Status { get; set; }

        public Guid? QuotationId { get; set; }
        public bool IsLead { get; set; }
        public string  Currency { get; set; }

        public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } = new();
    }

    public class SupplierQuotationItemDto
    {
        public decimal QuotedPrice { get; set; }

        public Guid? ItemQuotationId { get; set; }
          public decimal? DeliveryCharge { get; set; }
            public string? DeliveryType { get; set; }

            public decimal? Discount { get; set; }
            public string? DiscountType { get; set; }

            public decimal? Tax { get; set; }
            public string? TaxType { get; set; }

            public decimal QuotedAmount { get; set; }
            public decimal SubTotal { get; set; }
            public int LineNumber { get; set; }
    }
}