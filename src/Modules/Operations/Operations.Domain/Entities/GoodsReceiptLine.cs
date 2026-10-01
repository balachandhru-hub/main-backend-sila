using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Line of a goods receipt. PurchaseOrderItemId is a plain reference.
    /// </summary>
    public class GoodsReceiptLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("GoodsReceipt")]
        public Guid GoodsReceiptId { get; set; }

        public GoodsReceipt GoodsReceipt { get; set; } = null!;

        [Required]
        public Guid PurchaseOrderItemId { get; set; }

        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(150)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal OpenQuantityBefore { get; set; }

        [Precision(18, 4)]
        public decimal? InvoiceQuantity { get; set; }

        [Precision(18, 4)]
        public decimal ReceivedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal AcceptedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal DamagedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal RejectedQuantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string Uom { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? BatchNumber { get; set; }

        public DateOnly? ExpiryDate { get; set; }
    }
}
