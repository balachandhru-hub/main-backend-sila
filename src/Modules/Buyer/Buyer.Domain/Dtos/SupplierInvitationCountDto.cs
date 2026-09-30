namespace Buyer.Domain.Dto
{
    public class SupplierInvitationSummaryDto
    {
        public int All { get; set; }

        public int Submitted { get; set; }

        public int Pending { get; set; }

        public int Accepted { get; set; }

        public int Declined { get; set; }
    }
}