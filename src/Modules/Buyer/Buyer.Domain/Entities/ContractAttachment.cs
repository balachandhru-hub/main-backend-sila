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
        [ForeignKey("Contract")]
        public Guid ContractId { get; set; }
        public Contract Contract { get; set; }

        public Guid AssetId { get; set; }
        public string? Type { get; set; }
        public ContractAttachment() { }


    }
}
