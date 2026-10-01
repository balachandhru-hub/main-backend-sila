using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Goods receipt (GRN). PurchaseOrderId, InvoiceId, SupplierId and OperatingUnitId are plain references. CreatedBy is the receiving user.
    /// </summary>
    public class GoodsReceipt : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        public Guid OperatingUnitId { get; set; }

        [Required]
        [MaxLength(150)]
        public string GrnNumber { get; set; } = string.Empty;

        [Required]
        public Guid PurchaseOrderId { get; set; }

        public Guid? InvoiceId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        public DateTime ReceiptDate { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public GoodsReceiptStatus Status { get; set; }

        [Required]
        [MaxLength(100)]
        public string PostingProvider { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ExternalReference { get; set; }

        public DateTime? PostedAt { get; set; }

        [MaxLength(100)]
        public string? FailureCode { get; set; }

        [MaxLength(2000)]
        public string? FailureMessage { get; set; }

        [MaxLength(40)]
        public string? BusinessStatus { get; set; }

        [MaxLength(40)]
        public string? ErpPostingStatus { get; set; }

        [MaxLength(100)]
        public string? ErpMaterialDocument { get; set; }

        [MaxLength(10)]
        public string? ErpDocumentYear { get; set; }

        public string? ErpResponseJson { get; set; }

        public DateTime? ErpPostedAt { get; set; }

        public int ErpAttemptCount { get; set; }

        public DateTime? LastErpAttemptAt { get; set; }
    }
}
