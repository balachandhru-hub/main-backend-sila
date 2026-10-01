using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Line of an invoice. MaterialId and PurchaseOrderItemId are plain references.
    /// </summary>
    public class InvoiceLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Invoice")]
        public Guid InvoiceId { get; set; }

        public Invoice Invoice { get; set; } = null!;

        public int LineNumber { get; set; }

        [MaxLength(150)]
        public string? SupplierMaterialCode { get; set; }

        public Guid? MaterialId { get; set; }

        [MaxLength(150)]
        public string? MaterialCodeRaw { get; set; }

        [Required]
        [MaxLength(1000)]
        public string DescriptionRaw { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal? Quantity { get; set; }

        [MaxLength(50)]
        public string? Uom { get; set; }

        [Precision(18, 4)]
        public decimal? UnitPrice { get; set; }

        [Precision(10, 4)]
        public decimal? TaxRate { get; set; }

        [Precision(18, 4)]
        public decimal? TaxAmount { get; set; }

        [Precision(18, 4)]
        public decimal? LineAmount { get; set; }

        [Precision(18, 4)]
        public decimal? DiscountAmount { get; set; }

        [Precision(18, 4)]
        public decimal? GrossAmount { get; set; }

        [MaxLength(100)]
        public string? PoItemNumber { get; set; }

        [MaxLength(150)]
        public string? BatchNumber { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        [Precision(5, 4)]
        public decimal? Confidence { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public InvoiceLineMatchStatus MatchStatus { get; set; }

        public Guid? PurchaseOrderItemId { get; set; }
    }
}
