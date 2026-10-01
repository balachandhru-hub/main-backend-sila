using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Item of a purchase order with its ordered, received and open quantity.
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

        [MaxLength(50)]
        public string? ItemNumber { get; set; }

        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(150)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal OrderedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal ReceivedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal OpenQuantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string Uom { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal? UnitPrice { get; set; }

        [Precision(18, 4)]
        public decimal? PriceQuantity { get; set; }

        [Precision(18, 4)]
        public decimal? ItemAmount { get; set; }

        [MaxLength(30)]
        public string? TaxCode { get; set; }

        [Precision(18, 4)]
        public decimal? TaxAmount { get; set; }

        [Precision(18, 4)]
        public decimal? GrossItemAmount { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(100)]
        public string? MaterialGroup { get; set; }

        [MaxLength(100)]
        public string? Plant { get; set; }

        [MaxLength(100)]
        public string? StorageLocation { get; set; }

        [MaxLength(50)]
        public string? ItemCategory { get; set; }

        [MaxLength(50)]
        public string? AccountAssignmentCategory { get; set; }

        public bool GoodsReceiptExpected { get; set; } = true;

        public bool InvoiceExpected { get; set; } = true;

        public bool DeliveryCompleted { get; set; }

        public bool DeletionIndicator { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public PurchaseOrderItemStatus Status { get; set; }

        [MaxLength(250)]
        public string? ExternalId { get; set; }

        public DateTime? SourceLastChangedAt { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        [MaxLength(128)]
        public string? SourceHash { get; set; }
    }
}
