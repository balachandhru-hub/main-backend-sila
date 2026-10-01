using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Supplier invoice captured from a document. SupplierId, PurchaseOrderId and OperatingUnitId are plain references.
    /// </summary>
    public class Invoice : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        [Required]
        [ForeignKey("Document")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        public Guid? SupplierId { get; set; }

        [Required]
        [MaxLength(150)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateOnly? InvoiceDate { get; set; }

        [MaxLength(500)]
        public string? SupplierNameRaw { get; set; }

        [MaxLength(150)]
        public string? SupplierTaxNumberRaw { get; set; }

        [MaxLength(150)]
        public string? PoNumberRaw { get; set; }

        public Guid? PurchaseOrderId { get; set; }

        public bool NoPurchaseOrder { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [Precision(18, 4)]
        public decimal? NetAmount { get; set; }

        [Precision(18, 4)]
        public decimal? TaxAmount { get; set; }

        [Precision(18, 4)]
        public decimal? GrossAmount { get; set; }

        [MaxLength(500)]
        public string? SupplierLegalName { get; set; }

        [MaxLength(1000)]
        public string? SupplierAddress { get; set; }

        [MaxLength(320)]
        public string? SupplierEmail { get; set; }

        [MaxLength(100)]
        public string? SupplierPhone { get; set; }

        [Precision(18, 4)]
        public decimal? DiscountAmount { get; set; }

        [Precision(18, 4)]
        public decimal? FreightAmount { get; set; }

        [Precision(18, 4)]
        public decimal? OtherCharges { get; set; }

        [Precision(18, 4)]
        public decimal? TaxableAmount { get; set; }

        [Precision(18, 4)]
        public decimal? AmountDue { get; set; }

        [MaxLength(500)]
        public string? PaymentTerms { get; set; }

        public DateOnly? DueDate { get; set; }

        [MaxLength(40)]
        public string? ExtractionStatus { get; set; }

        [MaxLength(100)]
        public string? ExtractionProvider { get; set; }

        public DateTime? ExtractedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public Guid? ReviewedByUserId { get; set; }

        public string? ManualEditedFieldsJson { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public InvoiceType InvoiceType { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public InvoiceStatus Status { get; set; }

        [Precision(5, 4)]
        public decimal? OverallConfidence { get; set; }
    }
}
