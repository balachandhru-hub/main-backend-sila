namespace Buyer.Domain.Dto
{
    public class BidCompareResponseDto
    {
        public Guid RFQId { get; set; }

        public string? RFQNumber { get; set; }
         public bool AddLotOption { get; set; }

        public List<BidCompareSupplierDto> Suppliers { get; set; } = new();
           public List<BidCompareRFQItemDto> RFQItems { get; set; } = new();

    }

    public class BidCompareSupplierDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public BidCompareQuotationDto FirstVersion { get; set; }

        public BidCompareQuotationDto LatestVersion { get; set; }
    }

    public class BidCompareQuotationDto
    {
        public Guid SupplierRFQId { get; set; }

        public Guid QuotationId { get; set; }

        public string Version { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? DiscountType { get; set; }

        public string? TaxType { get; set; }

        public string Status { get; set; }

        public List<BidCompareItemDto> Items { get; set; } = new();
    }

    public class BidCompareItemDto
    {
        public Guid SupplierQuotationItemId { get; set; }

        public Guid SupplierRFQItemId { get; set; }

        public Guid BuyerRFQItemId { get; set; }

        public string Version { get; set; }

        public decimal QuotedPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? DiscountType { get; set; }

        public string? TaxType { get; set; }

        public decimal QuotedAmount { get; set; }

        public decimal SubTotal { get; set; }

        public int LineNumber { get; set; }
    }
    public class BidCompareRFQItemDto
{
    public Guid Id { get; set; }
    public string Description { get; set; }
    public decimal Quantity { get; set; }
    public string UOM { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialGroup { get; set; }
    public string? CostCenter { get; set; }
    public int LineNumber { get; set; }
}
}
