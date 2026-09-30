using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class WishlistItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Wishlist")]
        public Guid WishlistId { get; set; }

        public Wishlist Wishlist { get; set; } = null!;

        [Required]
        public Guid MaterialId { get; set; }

        [Required]
        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string? UnitOfMeasure { get; set; }

        public decimal Quantity { get; set; }

        public decimal? UnitPrice { get; set; }

        public string? Currency { get; set; }

        public DateTime? RequiredDate { get; set; }
    }
}
