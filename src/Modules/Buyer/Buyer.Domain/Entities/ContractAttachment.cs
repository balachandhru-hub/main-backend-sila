using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ContractAttachment : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("ContractDetails")]
        public Guid ContractDetailsId { get; set; }
        public ContractDetails ContractDetails { get; set; }

        public Guid AssetId { get; set; }
    

        public ContractAttachment() { }
    }
}
