using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Supplier.Domain.Entities
{
    public class SupplierQuotationHistory : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
 
        [Required]
        [ForeignKey("SupplierQuotation")]
        public Guid SupplierQuotationId { get; set; }
        public SupplierQuotation SupplierQuotation { get; set; }

      
        public Guid SupplierRFQId { get; set; }
        

        [Required]
        public Guid BuyerRFQId { get; set; }

        [Required]
        public string RFQNumber { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        [Required]
        public string Version { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? DiscountType { get; set; }

        public string? TaxType { get; set; }

        public string Status { get; set; }

        public SupplierQuotationHistory()
        {
        }
    }
}