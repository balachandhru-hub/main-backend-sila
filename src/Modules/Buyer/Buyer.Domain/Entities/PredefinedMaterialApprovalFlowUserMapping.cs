using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class PredefinedMaterialApprovalFlowUserMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("ApprovalFlowPredefinedMaterialMapping")]
    
        public Guid ApprovalFlowPredefinedMaterialId {get; set; }
        public ApprovalFlowPredefinedMaterialMapping ApprovalFlowPredefinedMaterialMapping {get; set; }
 
        public Guid ApprovalFlowId { get; set; }

      
        public Guid UserId { get; set; }

        public int Order { get; set; }

        public string Comment { get; set; }
        public string Status { get; set; }

        public PredefinedMaterialApprovalFlowUserMapping() { }
    }
}
