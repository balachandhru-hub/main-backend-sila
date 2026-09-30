namespace Buyer.Domain.Dto
{
    public class ApprovalKpiDto
    {
        public int TotalCount { get; set; }

        public int PendingCount { get; set; }

        public int ApprovedCount { get; set; }

        public int RejectedCount { get; set; }
    }
}
