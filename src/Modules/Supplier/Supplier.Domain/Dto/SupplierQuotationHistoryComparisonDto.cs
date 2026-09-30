

 namespace Supplier.Domain.Dto
{
    public class SupplierQuotationHistoryComparisonDto
    {
        public Guid SupplierQuotationId { get; set; }

        public string OldVersion { get; set; }

        public string LatestVersion { get; set; }

        public decimal OldTotalPrice { get; set; }
           
   

        public decimal LatestTotalPrice { get; set; }
         public decimal TotalPriceDifference { get; set; }

        public decimal? OldDiscount { get; set; }

        public decimal? LatestDiscount { get; set; }
           public decimal? DiscountDifference { get; set; }

        public string? OldDiscountType { get; set; }

        public string? LatestDiscountType { get; set; }

        public decimal? OldTax { get; set; }

        public decimal? LatestTax { get; set; }
           public decimal? TaxDifference { get; set; }

        public string? OldTaxType { get; set; }

        public string? LatestTaxType { get; set; }

        public decimal? OldDeliveryCharge { get; set; }

        public decimal? LatestDeliveryCharge { get; set; }
        public decimal? DeliveryChargeDifference { get; set; }

        public string? OldDeliveryType { get; set; }

        public string? LatestDeliveryType { get; set; }

        public List<SupplierQuotationItemComparisonDto> Items { get; set; }=new List<SupplierQuotationItemComparisonDto>();
         
    }

    public class SupplierQuotationItemComparisonDto
    {
        public Guid SupplierQuotationItemId { get; set; }

        public Guid SupplierRFQItemId { get; set; }

        public string OldVersion { get; set; }

        public decimal OldQuotedPrice { get; set; }

        public string LatestVersion { get; set; }

        public decimal LatestQuotedPrice { get; set; }

        public decimal PriceDifference { get; set; }

        public bool PriceChanged { get; set; }
    }
}