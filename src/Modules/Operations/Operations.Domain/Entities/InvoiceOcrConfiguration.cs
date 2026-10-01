using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Invoice OCR policy of an organization (one row per organization).
    /// </summary>
    public class InvoiceOcrConfiguration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public bool MobileBasicOcrEnabled { get; set; } = true;

        public bool AutomaticBackendFallbackEnabled { get; set; } = true;

        [Precision(5, 4)]
        public decimal MinimumMobileConfidence { get; set; } = 0.75m;

        public bool RequireSupplierName { get; set; } = true;

        public bool RequireInvoiceNumber { get; set; } = true;

        public bool RequirePurchaseOrderNumber { get; set; } = true;

        public bool RequireInvoiceAmount { get; set; } = true;

        public bool RequireInvoiceDate { get; set; }

        public bool RequireCurrency { get; set; }

        public bool RequireSupplierTrn { get; set; }

        [Required]
        [MaxLength(100)]
        public string BackendProvider { get; set; } = "BUILT_IN_ADVANCED";

        public bool AlwaysBackendOnReread { get; set; } = true;

        public bool DetailedLineExtractionEnabled { get; set; } = true;

        public bool SupplierMasterValidationEnabled { get; set; } = true;

        public bool PurchaseOrderValidationEnabled { get; set; } = true;

        public bool FinancialReconciliationEnabled { get; set; } = true;

        [Precision(18, 4)]
        public decimal AmountTolerance { get; set; } = 0.05m;

        public int BackendTimeoutSeconds { get; set; } = 60;

        public int BackendRetryCount { get; set; } = 1;

        public bool ReuseCachedOcr { get; set; } = true;

        public int Version { get; set; } = 1;
    }
}
