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

        // No longer a hard FK to PredefinedMaterial: for the manual flow
        // this stores a PredefinedMaterial.Id; for the Excel bulk flow
        // (UploadType = EXCEL) it stores an ExcelMaterialMaster.Id
        // instead - see UploadType below.
        [Required]
        public Guid PredefinedMaterialId { get; set; }

        /// <summary>
        /// Distinguishes which table PredefinedMaterialId points to:
        /// "MANUAL" (existing single-material flow -&gt; PredefinedMaterial)
        /// or "EXCEL" (bulk Excel upload flow -&gt; ExcelMaterialMaster).
        /// </summary>
        public string UploadType { get; set; }

        public ApprovalFlowPredefinedMaterialMapping() { }
    }
}
