namespace Supplier.Domain.Dto
{
    public class UpdateSupplierRFQStatusDto
    {
        public Guid RFQId { get; set; }
        public string Status { get; set; }
    }
}