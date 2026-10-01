using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class BuyerOutlet : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public string OutletName { get; set; } = string.Empty;

        public string? OutletCode { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Ship-to identifier sent to a supplier ERP. Configured per outlet, never hardcoded.
        /// </summary>
        public string? ExternalShipTo { get; set; }

        public string? AddressLine1 { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        /// <summary>
        /// Approval flow (type WISHLIST) used by every wishlist of this outlet.
        /// The flow is a MasterApprovalFlow id; kept as a reference so an outlet can exist before its flow.
        /// </summary>
        public Guid? MasterApprovalFlowId { get; set; }
    }
}
