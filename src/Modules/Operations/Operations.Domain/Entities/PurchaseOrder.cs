using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Purchase order imported from the ERP or a spreadsheet. SupplierId is a plain reference to the supplier master.
    /// </summary>
    public class PurchaseOrder : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        [Required]
        [MaxLength(150)]
        public string PoNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PurchaseOrderType { get; set; }

        [MaxLength(50)]
        public string? CompanyCode { get; set; }

        [MaxLength(100)]
        public string? ErpSupplierId { get; set; }

        [MaxLength(250)]
        public string? SupplierName { get; set; }

        [MaxLength(100)]
        public string? PurchasingOrganization { get; set; }

        [MaxLength(100)]
        public string? PurchasingGroup { get; set; }

        [MaxLength(100)]
        public string? PaymentTerms { get; set; }

        [MaxLength(50)]
        public string? PoCategory { get; set; }

        public DateOnly? PoDate { get; set; }

        public DateOnly? DeliveryDate { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal? TotalNetAmount { get; set; }

        [Precision(18, 4)]
        public decimal? TotalTaxAmount { get; set; }

        [Precision(18, 4)]
        public decimal? TotalAmount { get; set; }

        [Precision(18, 4)]
        public decimal? TotalOrderedQuantity { get; set; }

        [Precision(18, 4)]
        public decimal? TotalReceivedQuantity { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public PurchaseOrderStatus Status { get; set; }

        [Required]
        [MaxLength(50)]
        public string SourceSystem { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? ExternalId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityCode { get; set; } = "DEFAULT";

        public Guid? SourceConfigurationId { get; set; }

        public DateTime? SourceLastChangedAt { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        [MaxLength(128)]
        public string? SourceHash { get; set; }
    }
}
