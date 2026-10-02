using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// One line of a purchase order.
    /// </summary>
    public class PurchaseOrderItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("PurchaseOrder")]
        public Guid PurchaseOrderId { get; set; }

        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int LineNumber { get; set; }

        /// <summary>
        /// Supplier catalog id. The catalog lives in the Supplier service, so this is a reference.
        /// </summary>
        public Guid CatalogId { get; set; }

        public string? Sku { get; set; }

        public string? MaterialCode { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        public string? UnitOfMeasure { get; set; }

        [Precision(18, 4)]
        public decimal? UnitPrice { get; set; }

        [Precision(9, 4)]
        public decimal? DiscountPercent { get; set; }

        [Precision(18, 4)]
        public decimal LineAmount { get; set; }

        public string? Currency { get; set; }

        /// <summary>
        /// BuyerOutlet id the line was requested for. Kept as a reference, not a foreign key.
        /// </summary>
        public Guid? OutletId { get; set; }

        public string? StorageLocation { get; set; }
    }
}
