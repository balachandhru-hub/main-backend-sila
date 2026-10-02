using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// One purchasing bucket per property per business week, shared by the outlet managers of the property.
    /// Once it leaves the OPEN status it is read-only.
    /// </summary>
    public class WeeklyBucket : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerOrganizationId { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        /// <summary>
        /// Week number followed by "WB-" and the plant code, for example 37WB-JVC.
        /// </summary>
        [Required]
        public string BucketCode { get; set; } = string.Empty;

        public int WeekNumber { get; set; }

        public int Year { get; set; }

        /// <summary>
        /// BuyerProperty id. Kept as a reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid PropertyId { get; set; }

        [Required]
        public string PlantCode { get; set; } = string.Empty;

        [Required]
        public string CompanyCode { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = string.Empty;

        public Guid? MasterApprovalFlowId { get; set; }

        public string? ApprovalName { get; set; }

        public Guid? FrozenBy { get; set; }

        public DateTime? FrozenOn { get; set; }

        public DateTime? FinalApprovedOn { get; set; }

        public string? LastError { get; set; }
    }
}
