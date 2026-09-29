using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class PredefinedContractAttachment : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("Contract")]
        public Guid ContractId { get; set; }
        public PredefinedContract Contract { get; set; }

        public Guid AssetId { get; set; }
        public string? Type { get; set; }

        public string Title { get; set; }
        public Guid? UnspscId { get; set; }
        public long? SegmentId { get; set; }
        public string? SegmentTitle { get; set; }
        public PredefinedContractAttachment() { }


    }
}
