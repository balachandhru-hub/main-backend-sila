namespace Supplier.Domain.Dto
{
    public class SupplierListDto
    {
        public Guid SupplierId { get; set; }

        public string SupplierName { get; set; }

        public string? SupplierCode { get; set; }

        public decimal Price { get; set; }

        public bool IsVerified { get; set; }

    }
}