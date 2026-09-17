using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQAwardItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("RFQAward")]
        public Guid RFQAwardId { get; set; }
        public RFQAward RFQAward { get; set; }

        [Required]
        [ForeignKey("RFQItem")]
        public Guid RFQItemId { get; set; }
        public RFQItem RFQItem { get; set; }

        public Guid SupplierId { get; set; }

        public Guid? SupplierQuotationId { get; set; }

        public Guid? SupplierQuotationItemId { get; set; }

        public string? QuotationVersion { get; set; }

        public RFQAwardItem() { }
    }
}
