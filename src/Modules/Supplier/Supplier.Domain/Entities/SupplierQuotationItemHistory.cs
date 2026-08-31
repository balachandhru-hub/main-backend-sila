using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;
using System.ComponentModel.DataAnnotations.Schema;
namespace Supplier.Domain.Entities
{
    public class SupplierQuotationItemHistory : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        

        public Guid SupplierQuotationItemId { get; set; }
       

       [Required]
        [ForeignKey("SupplierQuotation")]
        public Guid SupplierQuotationId { get; set; }
        public SupplierQuotation SupplierQuotation { get; set; }

        [Required]
        public Guid SupplierRFQItemId { get; set; }
    

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

        [Required]
        public string Version { get; set; }

        public decimal QuotedPrice { get; set; }

        public SupplierQuotationItemHistory()
        {
        }
    }
}