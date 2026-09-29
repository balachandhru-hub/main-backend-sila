using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ContractTemplate : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public long SegmentId { get; set; }

        public string? SegmentTitle { get; set; }

        public string? TemplateName { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        public Guid AssetId { get; set; }

        public ContractTemplate() { }
    }
}
