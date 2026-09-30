using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class RFQAttachmentMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerRFQId { get; set; }

        [Required]
        [ForeignKey("SupplierRFQ")]
        public Guid SupplierRFQId { get; set; }

        public SupplierRFQ SupplierRFQ { get; set; }
        public Guid SupplierId { get; set; }
        [Required]
        public Guid AssetId { get; set; }

        [Required]
        public string Type { get; set; }


        public RFQAttachmentMapping()
        {
        }
    }
}
