namespace Supplier.Domain.Dto
{
    public class UpdateSupplierStatusDto
    {
        public Guid OrganizationId { get; set; }

        public bool IsActive { get; set; }
    }
}