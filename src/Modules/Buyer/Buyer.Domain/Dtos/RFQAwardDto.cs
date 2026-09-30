using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
{
    public class SaveRFQAwardDto
    {
        [Required]
        public Guid RFQId { get; set; }

        public string? Remarks { get; set; }

        [Required]
        public List<RFQAwardSelectionDto> Selections { get; set; } = new();
    }

    public class RFQAwardSelectionDto
    {
        [Required]
        public Guid RFQItemId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }
    }

    public class RFQAwardResponseDto
    {
        public Guid Id { get; set; }

        public Guid RFQId { get; set; }

        public string? RFQNumber { get; set; }

        public Guid BuyerId { get; set; }

        public decimal TotalAwardValue { get; set; }

        public int AwardedItems { get; set; }

        public int AwardedSuppliers { get; set; }

        public string? Currency { get; set; }

        public string? Status { get; set; }

        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; }
        public decimal? Tax { get; set; }
        public string? TaxType { get; set; }
        public decimal? DeliveryCharge { get; set; }
        public string? DeliveryType { get; set; }

        public string? Remarks { get; set; }

        public DateTime AwardedOn { get; set; }

        public List<RFQAwardItemDto> Items { get; set; } = new();
    }

    public class RFQAwardItemDto
    {
        public Guid Id { get; set; }

        public Guid RFQItemId { get; set; }

        public int LineNumber { get; set; }

        public Guid SupplierId { get; set; }

        public Guid? SupplierRFQId { get; set; }

        public Guid? SupplierQuotationId { get; set; }

        public Guid? SupplierQuotationItemId { get; set; }

        public Guid? SupplierRFQItemId { get; set; }

        public string? QuotationVersion { get; set; }

        public decimal Quantity { get; set; }

        public decimal? QuotedPrice { get; set; }

        public decimal? QuotedAmount { get; set; }

        public decimal? Discount { get; set; }
        public string? DiscountType { get; set; }

        public decimal? Tax { get; set; }
        public string? TaxType { get; set; }

        public decimal? DeliveryCharge { get; set; }
        public string? DeliveryType { get; set; }

        public decimal? SubTotal { get; set; }
    }
}
