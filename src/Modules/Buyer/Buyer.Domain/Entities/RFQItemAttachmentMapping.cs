using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class RFQItemAttachmentMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("RFQItem")]
        public Guid RFQItemId { get; set; }
        public RFQItem RFQItem { get; set; }

        public Guid AssetId { get; set; }
        public string? Type { get; set; }
        public RFQItemAttachmentMapping() { }


    }
}
