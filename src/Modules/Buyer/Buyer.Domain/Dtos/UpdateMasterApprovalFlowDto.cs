namespace Buyer.Domain.Dtos
{
    public class UpdateMasterApprovalFlowDto
    {
        public string ApprovalCode { get; set; }

        public string ApprovalName { get; set; }
        public int Order { get; set; }
    }
}
