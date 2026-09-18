using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ContractApprovalFlow : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string ApprovalCode { get; set; }

        public string ApprovalName { get; set; }

        [Required]
        [ForeignKey("Contract")]
        public Guid ContractId { get; set; }

        public Contract Contract { get; set; }

        public string Type { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }

        public ContractApprovalFlow() { }
    }
}
