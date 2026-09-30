namespace Supplier.Domain.Dto
{
    public class CreateSupplierQuotationItemDto
    {
        public Guid SupplierRFQItemId { get; set; }
        public Guid BuyerRFQItemId { get; set; }
        public decimal QuotedPrice { get; set; }
        public decimal? DeliveryCharge { get; set; }
        public string? DeliveryType { get; set; }

        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; }

        public decimal? Tax { get; set; }
        public string? TaxType { get; set; }

        /// <summary>
        /// True when the supplier does not have this line item's
        /// product/service to quote for. Set per line item, not per
        /// quotation.
        /// </summary>
        public bool ISLineitemAvailable { get; set; }
    }
}