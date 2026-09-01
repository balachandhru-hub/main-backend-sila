namespace Buyer.Domain.Dto
{
    public class SupplierInvitationSummaryDto
    {
        public int All { get; set; }

        public int Draft { get; set; }

        public int Pending { get; set; }

        public int Accepted { get; set; }

        public int Declined { get; set; }
    }
}