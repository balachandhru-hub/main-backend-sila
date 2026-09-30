namespace Buyer.Domain.Dto
{
    /// <summary>
    /// Whether the buyer has accepted this supplier's terms and condition,
    /// for one supplier invited to the RFQ. One row per supplier.
    /// </summary>
    public class SupplierTermsAndConditionAcceptedDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public string SupplierTermsAndConditionAccepted { get; set; }
    }
}
