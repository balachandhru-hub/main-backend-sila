namespace Operations.Domain.Dtos
{
    public class AdvancedInvoiceLineResponseDto
    {
        public int LineNumber { get; set; }
        public AdvancedExtractedFieldDto<string> SupplierItemCode { get; set; } = new();
        public AdvancedExtractedFieldDto<string> Description { get; set; } = new();
        public AdvancedExtractedFieldDto<string> PoItemNumber { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> Quantity { get; set; } = new();
        public AdvancedExtractedFieldDto<string> Uom { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> UnitPrice { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> DiscountAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> NetAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> TaxRate { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> TaxAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<decimal?> GrossAmount { get; set; } = new();
        public AdvancedExtractedFieldDto<string> BatchNumber { get; set; } = new();
        public AdvancedExtractedFieldDto<DateOnly?> ExpiryDate { get; set; } = new();
    }
}
