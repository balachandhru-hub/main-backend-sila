using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ContractApprovalUserMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("ContractApprovalFlow")]
        public Guid ContractApprovalFlowId { get; set; }

        public ContractApprovalFlow ContractApprovalFlow { get; set; }

        public Guid UserId { get; set; }

        public int Order { get; set; }

        public string Comment { get; set; }
        public string Status { get; set; }

        public ContractApprovalUserMapping() { }
    }
}
