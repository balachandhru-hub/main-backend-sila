using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierQuotation : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(SupplierRFQ))]
        public Guid SupplierRFQId { get; set; }

        public SupplierRFQ SupplierRFQ { get; set; }

        [Required]
        public Guid BuyerRFQId { get; set; }

        [Required]
        public string RFQNumber { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }
        public string? DiscountType { get; set; }  
        public string? TaxType { get; set; } 

        public string Status { get; set; }

        public SupplierQuotation()
        {
        }
    }
}