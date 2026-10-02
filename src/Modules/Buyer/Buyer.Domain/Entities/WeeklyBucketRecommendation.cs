using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// An alternative product proposed for a bucket line that the supplier cannot fully supply.
    /// </summary>
    public class WeeklyBucketRecommendation : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("WeeklyBucket")]
        public Guid WeeklyBucketId { get; set; }

        public WeeklyBucket WeeklyBucket { get; set; } = null!;

        /// <summary>
        /// WeeklyBucketItem id. Kept as a reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid WeeklyBucketItemId { get; set; }

        /// <summary>
        /// R1, R2, ... running per bucket.
        /// </summary>
        [Required]
        public string RecommendationNumber { get; set; } = string.Empty;

        /// <summary>
        /// Number part of RecommendationNumber, used to find the next one.
        /// </summary>
        public int Sequence { get; set; }

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
        public decimal? AvailableStock { get; set; }

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public Guid? DecidedBy { get; set; }

        public DateTime? DecidedOn { get; set; }
    }
}
