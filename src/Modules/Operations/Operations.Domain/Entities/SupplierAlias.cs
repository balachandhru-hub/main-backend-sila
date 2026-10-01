using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Alternative name of a supplier, used to match the supplier name read from an invoice.
    /// </summary>
    public class SupplierAlias : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        public Guid SupplierId { get; set; }

        public SupplierMaster Supplier { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string Alias { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string NormalizedAlias { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SourceSystem { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityCode { get; set; } = "DEFAULT";

        [Precision(5, 4)]
        public decimal? Confidence { get; set; }

        public bool IsConfirmed { get; set; }
    }
}
