namespace Supplier.Domain.Dto
{
    public class ExternalSupplierNameDto
    {
        public Guid ExternalSupplierId { get; set; }

        public string? ExternalSupplierName { get; set; }

        public string? SupplierType { get; set; }
    }
}
