namespace Operations.Domain.Dtos
{
    public class InvoiceOcrConfigurationResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public bool MobileBasicOcrEnabled { get; set; }
        public bool AutomaticBackendFallbackEnabled { get; set; }
        public decimal MinimumMobileConfidence { get; set; }
        public bool RequireSupplierName { get; set; }
        public bool RequireInvoiceNumber { get; set; }
        public bool RequirePurchaseOrderNumber { get; set; }
        public bool RequireInvoiceAmount { get; set; }
        public bool RequireInvoiceDate { get; set; }
        public bool RequireCurrency { get; set; }
        public bool RequireSupplierTrn { get; set; }
        public string BackendProvider { get; set; } = string.Empty;
        public bool AlwaysBackendOnReread { get; set; }
        public bool DetailedLineExtractionEnabled { get; set; }
        public bool SupplierMasterValidationEnabled { get; set; }
        public bool PurchaseOrderValidationEnabled { get; set; }
        public bool FinancialReconciliationEnabled { get; set; }
        public decimal AmountTolerance { get; set; }
        public int BackendTimeoutSeconds { get; set; }
        public int BackendRetryCount { get; set; }
        public bool ReuseCachedOcr { get; set; }
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
