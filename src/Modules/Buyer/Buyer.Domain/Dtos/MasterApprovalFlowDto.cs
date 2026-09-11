namespace Buyer.Domain.Dtos
{
    public class MasterApprovalFlowDto
    {
        public Guid Id { get; set; }

        public string ApprovalCode { get; set; }

        public string ApprovalName { get; set; }

        public Guid BuyerId { get; set; }
    }
}
