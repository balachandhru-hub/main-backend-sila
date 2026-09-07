using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQExternalSupplier : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }

        public RFQ RFQ { get; set; }

        [Required]
        [ForeignKey("ExternalSupplier")]
        public Guid ExternalSupplierId { get; set; }

        public ExternalSupplier ExternalSupplier { get; set; }

        public string Status { get; set; }

        public RFQExternalSupplier() { }
    }
}
