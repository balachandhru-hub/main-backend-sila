using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Supplier master record of an organization (SILAME 'suppliers').
    /// </summary>
    public class SupplierMaster : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SupplierCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string NormalizedName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? SearchName { get; set; }

        [MaxLength(100)]
        public string? BusinessPartnerId { get; set; }

        [MaxLength(100)]
        public string? TaxNumber { get; set; }

        [MaxLength(100)]
        public string? Trn { get; set; }

        [MaxLength(250)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(100)]
        public string EntityCode { get; set; } = "DEFAULT";

        [MaxLength(250)]
        public string? LegalName { get; set; }

        [MaxLength(1000)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(150)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? PostalCode { get; set; }

        [MaxLength(250)]
        public string? Street { get; set; }

        [MaxLength(100)]
        public string? CompanyCode { get; set; }

        [MaxLength(100)]
        public string? PurchasingOrganization { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(100)]
        public string? PaymentTerms { get; set; }

        public bool IsBlocked { get; set; }

        public bool IsDeleted { get; set; }

        [Column(TypeName = "nvarchar(16)")]
        public StatusKind Status { get; set; } = StatusKind.ACTIVE;

        [MaxLength(100)]
        public string? SourceSystem { get; set; }

        public Guid? SourceConfigurationId { get; set; }

        public DateTime? SourceLastChangedAt { get; set; }

        public DateTime? LastSyncedAt { get; set; }

        [MaxLength(250)]
        public string? ExternalId { get; set; }
    }
}
