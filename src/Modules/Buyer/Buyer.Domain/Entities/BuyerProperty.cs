using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// A property (plant) of the buyer. Outlets belong to a property and a weekly bucket is raised per property.
    /// </summary>
    public class BuyerProperty : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string CompanyCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PlantCode { get; set; } = string.Empty;

        [Required]
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Approval flow (type WEEKLY_BUCKET) used when the bucket of this property is frozen.
        /// The flow is a MasterApprovalFlow id; kept as a reference so a property can exist before its flow.
        /// </summary>
        public Guid? MasterApprovalFlowId { get; set; }
    }
}
