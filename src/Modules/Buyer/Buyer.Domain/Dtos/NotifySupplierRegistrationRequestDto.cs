namespace Buyer.Domain.Dto
{
    public class NotifySupplierRegistrationRequestDto
    {
        public Guid ExternalSupplierId { get; set; }

        public Guid RFQId { get; set; }
    }
}
