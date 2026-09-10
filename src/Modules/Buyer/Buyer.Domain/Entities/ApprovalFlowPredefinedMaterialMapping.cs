using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ApprovalFlowPredefinedMaterialMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        
        public Guid ApprovalFlowId { get; set; }

        [Required]
        [ForeignKey("PredefinedMaterial")]
     
        public Guid PredefinedMaterialId { get; set; }

        public PredefinedMaterial PredefinedMaterial { get; set; }
      

        public ApprovalFlowPredefinedMaterialMapping() { }
    }
}
