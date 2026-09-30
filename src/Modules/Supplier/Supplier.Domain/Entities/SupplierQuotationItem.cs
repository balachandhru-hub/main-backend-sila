using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierQuotationItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(SupplierQuotation))]
        public Guid SupplierQuotationId { get; set; }

        public SupplierQuotation SupplierQuotation { get; set; }

        [Required]
        [ForeignKey(nameof(SupplierRFQItem))]
        public Guid SupplierRFQItemId { get; set; }

        public SupplierRFQItem SupplierRFQItem { get; set; }

        [Required]
        public Guid BuyerRFQItemId { get; set; }

        [Required]
        public Guid BuyerRFQId { get; set; }

        [Required]
        public string RFQNumber { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }
        public decimal QuotedPrice { get; set; }
        public decimal? DeliveryCharge { get; set; }
        public decimal? Tax { get; set; }
        public decimal? Discount { get; set; }
        public string? DeliveryType { get; set; }
        public string? DiscountType { get; set; }
        public string? TaxType { get; set; }
        public decimal QuotedAmount { get; set; }
        public decimal SubTotal { get; set; } 
        public bool ISLineitemAvailable { get; set; }
        public SupplierQuotationItem()
        {
        }
    }
}