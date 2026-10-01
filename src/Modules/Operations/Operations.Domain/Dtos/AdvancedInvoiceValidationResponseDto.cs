namespace Operations.Domain.Dtos
{
    public class AdvancedInvoiceValidationResponseDto
    {
        public bool AmountsReconciled { get; set; }
        public bool LineTotalMatchesNet { get; set; }
        public bool TaxReconciled { get; set; }
        public decimal? Difference { get; set; }
        public bool SupplierMatched { get; set; }
        public bool PurchaseOrderMatched { get; set; }
    }
}
