using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class PersonalWishlistItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("PersonalWishlist")]
        public Guid PersonalWishlistId { get; set; }

        public PersonalWishlist PersonalWishlist { get; set; } = null!;

        /// <summary>
        /// Supplier catalog id. The catalog lives in the Supplier service, so this is a reference.
        /// </summary>
        [Required]
        public Guid CatalogId { get; set; }

        public string? Sku { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public string? UnitOfMeasure { get; set; }

        [Precision(18, 4)]
        public decimal? Price { get; set; }

        public string? Currency { get; set; }

        [Precision(9, 4)]
        public decimal? DiscountPercent { get; set; }

        [Precision(18, 4)]
        public decimal Quantity { get; set; }
    }
}
