using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Current, denormalized extraction result of an invoice document: one row per invoice line (or one header-only row).
    /// </summary>
    public class InvoiceExtData : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Document")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        [MaxLength(500)]
        public string? SupplierName { get; set; }

        [MaxLength(150)]
        public string? SupplierTrn { get; set; }

        [MaxLength(150)]
        public string? SupplierInvoiceNumber { get; set; }

        public DateOnly? InvoiceDate { get; set; }

        [MaxLength(150)]
        public string? PurchaseOrderNumber { get; set; }

        [Precision(18, 4)]
        public decimal? InvoiceGross { get; set; }

        [Precision(18, 4)]
        public decimal? InvoiceNet { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(200)]
        public string? ItemSkuId { get; set; }

        [Precision(18, 4)]
        public decimal? ItemAmount { get; set; }

        [Precision(18, 4)]
        public decimal? ItemNet { get; set; }

        [MaxLength(1500)]
        public string? ItemDescription { get; set; }

        [MaxLength(100)]
        public string? LineItemNumber { get; set; }

        public Guid? PurchaseOrderItemId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SourceProvider { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ExtractionMethod { get; set; } = string.Empty;

        [Precision(10, 4)]
        public decimal? ExtractionConfidence { get; set; }
    }
}
