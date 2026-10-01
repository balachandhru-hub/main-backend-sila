using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class WishlistAudit : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Wishlist")]
        public Guid WishlistId { get; set; }

        public Wishlist Wishlist { get; set; } = null!;

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public Guid? ActorUserId { get; set; }
    }
}
