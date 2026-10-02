using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// One requested product of a weekly bucket. A line belongs to the user who requested it.
    /// </summary>
    public class WeeklyBucketItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("WeeklyBucket")]
        public Guid WeeklyBucketId { get; set; }

        public WeeklyBucket WeeklyBucket { get; set; } = null!;

        /// <summary>
        /// Supplier catalog id. The catalog lives in the Supplier service, so this is a reference.
        /// </summary>
        [Required]
        public Guid CatalogId { get; set; }

        public string? Sku { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Name of the product first requested, set when an approved recommendation replaced it.
        /// </summary>
        public string? OriginalProductName { get; set; }

        /// <summary>
        /// Item Master id, copied from the catalog-material mapping.
        /// </summary>
        public Guid? MaterialId { get; set; }

        public string? MaterialCode { get; set; }

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
        public decimal RequestedQuantity { get; set; }

        /// <summary>
        /// Final quantity. Equal to the requested quantity until a reviewer changes it.
        /// </summary>
        [Precision(18, 4)]
        public decimal ApprovedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal? SupplierStock { get; set; }

        public DateTime? SupplierStockRefreshedOn { get; set; }

        [Precision(18, 4)]
        public decimal? StockInHand { get; set; }

        [Required]
        public string AvailabilityStatus { get; set; } = string.Empty;

        [Required]
        public string LineStatus { get; set; } = string.Empty;

        /// <summary>
        /// Identity user id of the requestor. A reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid RequestorUserId { get; set; }

        /// <summary>
        /// BuyerOutlet id. Kept as a reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid OutletId { get; set; }

        public string? StorageLocation { get; set; }
    }
}
