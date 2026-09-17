namespace Buyer.Domain.Dto
{
    public class SupplierNameDto
    {
        public Guid SupplierId { get; set; }

        public string SupplierName { get; set; }

        /// <summary>"VERIFIED" or "UNVERIFIED" for this buyer - set locally, not part of the cross-service response.</summary>
        public string? VerificationStatus { get; set; }
    }
}
