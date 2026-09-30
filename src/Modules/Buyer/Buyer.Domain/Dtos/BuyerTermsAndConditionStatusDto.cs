namespace Buyer.Domain.Dto
{
    public class BuyerTermsAndConditionStatusDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public string BuyerTermsAndConditionAccepted { get; set; }

        /// <summary>
        /// True once the buyer has invited this supplier to contract, for this RFQ.
        /// </summary>
        public bool IsSupplierInvitedForContract { get; set; }
    }
}
