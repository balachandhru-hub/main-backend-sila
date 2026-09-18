namespace Supplier.Domain.Dto
{
    public class BuyerTermsAndConditionStatusDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public bool BuyerTermsAndConditionAccepted { get; set; }
    }
}
