using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class AdvancedInvoiceHeaderResponseDto
    {
        public AdvancedExtractedFieldDto<string> SupplierName { get; set; } = new();
        public AdvancedExtractedFieldDto<string> SupplierLegalName { get; set; } = new();
        public AdvancedExtractedFieldDto<string> SupplierTrn { get; set; } = new();
        public AdvancedExtractedFieldDto<string> SupplierAddress { get; set; } = new();
        public AdvancedExtractedFieldDto<string> SupplierEmail { get; set; } = new();
        public AdvancedExtractedFieldDto<string> SupplierPhone { get; set; } = new();
        public AdvancedExtractedFieldDto<string> InvoiceNumber { get; set; } = new();
        public AdvancedExtractedFieldDto<DateOnly?> InvoiceDate { get; set; } = new();
        public AdvancedExtractedFieldDto<string> InvoiceType { get; set; } = new();
        public AdvancedExtractedFieldDto<string> PurchaseOrderNumber { get; set; } = new();
        public AdvancedExtractedFieldDto<string> Currency { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> NetAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> DiscountAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> FreightAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> OtherCharges { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> TaxableAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> TaxAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> GrossAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> AmountDue { get; set; } = new();
        public AdvancedExtractedFieldDto<string> PaymentTerms { get; set; } = new();
        public AdvancedExtractedFieldDto<DateOnly?> DueDate { get; set; } = new();
    }
}
