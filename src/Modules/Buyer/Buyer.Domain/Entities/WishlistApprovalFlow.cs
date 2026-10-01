using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class WishlistApprovalFlow : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string ApprovalCode { get; set; } = string.Empty;

        public string ApprovalName { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Wishlist")]
        public Guid WishlistId { get; set; }

        public Wishlist Wishlist { get; set; } = null!;

        public Guid MasterApprovalFlowId { get; set; }

        public string Type { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string? Currency { get; set; }
    }
}
