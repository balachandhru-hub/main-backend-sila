using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQAward : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }
        public RFQ RFQ { get; set; }

        public Guid? SupplierQuotationId { get; set; }

        public Guid? SupplierId { get; set; }

        public string? QuotationVersion { get; set; }

        public string SelectionMode { get; set; }

        public string Status { get; set; }

        public string? Remarks { get; set; }

        public RFQAward() { }
    }
}
