using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ApprovalFlowUserMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("MasterApprovalFlow")]
        public Guid ApprovalFlowId { get; set; }

        public MasterApprovalFlow MasterApprovalFlow { get; set; }

        public Guid UserId { get; set; }

        public int Order { get; set; }

        public ApprovalFlowUserMapping() { }
    }
}
