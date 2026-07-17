using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class RFQAttachmentMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }
        public RFQ RFQ { get; set; }

        public Guid AssetId { get; set; }
        public string? Type { get; set; }
        public RFQAttachmentMapping() { }


    }
}
