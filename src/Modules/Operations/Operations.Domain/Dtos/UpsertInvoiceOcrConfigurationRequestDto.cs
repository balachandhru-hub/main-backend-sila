using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class UpsertInvoiceOcrConfigurationRequestDto
    {
        public bool MobileBasicOcrEnabled { get; set; } = true;
        public bool AutomaticBackendFallbackEnabled { get; set; } = true;
        [Range(0, 1)]
        public decimal MinimumMobileConfidence { get; set; } = 0.75m;
        public bool RequireSupplierName { get; set; } = true;
        public bool RequireInvoiceNumber { get; set; } = true;
        public bool RequirePurchaseOrderNumber { get; set; } = true;
        public bool RequireInvoiceAmount { get; set; } = true;
        public bool RequireInvoiceDate { get; set; }
        public bool RequireCurrency { get; set; }
        public bool RequireSupplierTrn { get; set; }
        [StringLength(100)]
        public string BackendProvider { get; set; } = "BUILT_IN_ADVANCED";
        public bool AlwaysBackendOnReread { get; set; } = true;
        public bool DetailedLineExtractionEnabled { get; set; } = true;
        public bool SupplierMasterValidationEnabled { get; set; } = true;
        public bool PurchaseOrderValidationEnabled { get; set; } = true;
        public bool FinancialReconciliationEnabled { get; set; } = true;
        [Range(0, 1000)]
        public decimal AmountTolerance { get; set; } = 0.05m;
        [Range(1, 600)]
        public int BackendTimeoutSeconds { get; set; } = 60;
        [Range(0, 10)]
        public int BackendRetryCount { get; set; } = 1;
        public bool ReuseCachedOcr { get; set; } = true;
    }
}
