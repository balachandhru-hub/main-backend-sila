namespace Buyer.Domain.Dtos
{
    public class CreateMasterApprovalFlowDto
    {
        public string ApprovalCode { get; set; }

        public string ApprovalName { get; set; }

        public string Type { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }

        public List<CreateApprovalFlowUserDto> Users { get; set; }
            = new List<CreateApprovalFlowUserDto>();
    }

    public class CreateApprovalFlowUserDto
    {
        public Guid UserId { get; set; }

        public int Order { get; set; }
    }
}