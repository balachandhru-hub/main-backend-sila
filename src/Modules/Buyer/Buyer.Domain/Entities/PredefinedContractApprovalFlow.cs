using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class PredefinedContractApprovalFlow : BaseModel
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

        public PredefinedContract Contract { get; set; }

        public string Type { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; }

        public PredefinedContractApprovalFlow() { }
    }
}
