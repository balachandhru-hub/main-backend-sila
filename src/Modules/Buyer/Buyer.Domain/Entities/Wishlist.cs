using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Organization-level purchasing list. CreatedBy is audit only and is not an ownership key.
    /// </summary>
    public class Wishlist : BaseModel
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

        [Required]
        [ForeignKey("Outlet")]
        public Guid OutletId { get; set; }

        public BuyerOutlet Outlet { get; set; } = null!;

        [Required]
        public string WishlistName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid? SupplierOrganizationId { get; set; }

        public string? SupplierName { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public Guid? MasterApprovalFlowId { get; set; }

        public string? ApprovalName { get; set; }

        public DateTime? SubmittedOn { get; set; }

        public Guid? SubmittedBy { get; set; }

        public DateTime? FinalApprovedOn { get; set; }

        public string? BuyerErpDocumentType { get; set; }

        public string? BuyerErpDocumentNumber { get; set; }

        public string? SupplierErpDocumentType { get; set; }

        public string? SupplierErpDocumentNumber { get; set; }

        public string? Currency { get; set; }

        public string? DeliveryInstruction { get; set; }

        public DateTime? RequiredDate { get; set; }

        public string? LastError { get; set; }

        public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
    }
}
