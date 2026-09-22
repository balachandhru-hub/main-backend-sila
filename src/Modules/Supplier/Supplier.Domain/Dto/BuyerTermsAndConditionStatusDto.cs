namespace Supplier.Domain.Dto
{
    public class BuyerTermsAndConditionStatusDto
    {
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        public bool BuyerTermsAndConditionAccepted { get; set; }

        /// <summary>
        /// True once the buyer has invited this supplier to contract
        /// (PUT api/v1/supplier/rfq/invite-for-contract), for this RFQ.
        /// </summary>
        public bool IsSupplierInvitedForContract { get; set; }
    }
}
