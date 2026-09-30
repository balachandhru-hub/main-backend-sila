using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class WishlistApprovalUserMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("WishlistApprovalFlow")]
        public Guid WishlistApprovalFlowId { get; set; }

        public WishlistApprovalFlow WishlistApprovalFlow { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        public int Order { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public DateTime? ActedOn { get; set; }
    }
}
