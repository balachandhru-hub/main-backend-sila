namespace Buyer.Domain.Dtos
{
    public class PredefinedContractApprovalFlowDto
    {
        public Guid Id { get; set; }

        public string ApprovalCode { get; set; }

        public string ApprovalName { get; set; }

        public Guid ContractId { get; set; }

        public string Type { get; set; }

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; }
    }
}
