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
        /// No longer used for approval: the weekly bucket takes its approval flow from the property.
        /// The column is kept so existing outlet rows are unchanged.
        /// </summary>
        public Guid? MasterApprovalFlowId { get; set; }

        /// <summary>
        /// BuyerProperty id of the property this outlet belongs to. Kept as a reference, not a foreign key.
        /// </summary>
        public Guid? PropertyId { get; set; }

        /// <summary>
        /// Storage location code of the outlet, for example J12. Sent on each purchase order line.
        /// </summary>
        public string? StorageLocation { get; set; }
    }
}
