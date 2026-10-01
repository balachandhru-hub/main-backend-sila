using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class UpdateInvoiceRequestDto
    {
        [Required, StringLength(150)]
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateOnly? InvoiceDate { get; set; }
        public string? SupplierName { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierTaxNumber { get; set; }
        public string? PoNumber { get; set; }
        public string? Currency { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? GrossAmount { get; set; }
        public bool NoPurchaseOrder { get; set; }
    }
}
