namespace Supplier.Domain.Dto
{
    public class ContractAttachmentDto
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }

        public string? Type { get; set; }

        public string? FileName { get; set; }
    }

    public class ContractApprovalFlowDto
    {
        public Guid Id { get; set; }

        public string? ApprovalCode { get; set; }

        public string? ApprovalName { get; set; }

        public Guid ContractId { get; set; }

        public string? Type { get; set; }

        public decimal TotalAmount { get; set; }

        public string? Currency { get; set; }
    }

    public class ContractResponseDto
    {
        public Guid Id { get; set; }

        public string? ContractNumber { get; set; }

        public string? ContractName { get; set; }

        public Guid RFQId { get; set; }

        public string? RFQNumber { get; set; }

        public string? RFQTitle { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public decimal Amount { get; set; }

        public DateTime DateCreated { get; set; }

        public List<ContractAttachmentDto> Attachments { get; set; } = new();

        public List<ContractApprovalFlowDto> ApprovalFlows { get; set; } = new();
    }
}
