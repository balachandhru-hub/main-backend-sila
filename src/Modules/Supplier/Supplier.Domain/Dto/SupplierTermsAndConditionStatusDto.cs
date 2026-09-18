namespace Supplier.Domain.Dto
{
    public class SupplierTermsAndConditionStatusDto
    {
        public Guid BuyerId { get; set; }

        public string? BuyerName { get; set; }

        public string Status { get; set; }
    }
}
