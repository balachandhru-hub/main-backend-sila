namespace Buyer.Domain.Dto
{
    public class RFQSupplierInviteDto
    {
        public Guid SupplierId { get; set; }

        public List<Guid> UserIds { get; set; } = new();
    }
}
