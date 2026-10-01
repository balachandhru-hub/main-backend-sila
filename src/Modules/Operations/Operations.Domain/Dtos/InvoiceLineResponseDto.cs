using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class InvoiceLineResponseDto
    {
        public Guid Id { get; set; }
        public int LineNumber { get; set; }
        public string? SupplierMaterialCode { get; set; }
        public Guid? MaterialId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public string? Uom { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TaxRate { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? LineAmount { get; set; }
        public decimal? Confidence { get; set; }
        public InvoiceLineMatchStatus MatchStatus { get; set; }
        public Guid? PurchaseOrderItemId { get; set; }
    }
}
