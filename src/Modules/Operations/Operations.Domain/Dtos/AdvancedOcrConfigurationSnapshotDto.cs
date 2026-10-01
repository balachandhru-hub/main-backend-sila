namespace Operations.Domain.Dtos
{
    public class AdvancedOcrConfigurationSnapshotDto
    {
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
        public bool AlwaysBackendOnReread { get; set; }
        public bool DetailedLineExtractionEnabled { get; set; }
        public bool SupplierMasterValidationEnabled { get; set; }
        public bool PurchaseOrderValidationEnabled { get; set; }
        public bool FinancialReconciliationEnabled { get; set; }
        public int Version { get; set; }
    }
}
