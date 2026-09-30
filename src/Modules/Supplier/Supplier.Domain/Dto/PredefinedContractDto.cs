namespace Supplier.Domain.Dto
{
    public class PredefinedContractAttachmentDto
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }

        public string? Type { get; set; }

        public string? FileName { get; set; }
    }

    public class PredefinedContractResponseDto
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

        public string? Status { get; set; }

        public List<PredefinedContractAttachmentDto> Attachments { get; set; } = new();

        // Only id, name, order and decision status are exposed to suppliers; approver emails and
        // the rest of the buyer-internal approval flow stay out of this DTO.
        public List<PredefinedContractApprovalUserDto> ApprovalUsers { get; set; } = new();
    }

    public class PredefinedContractApprovalUserDto
    {
        public Guid UserId { get; set; }

        public string? UserName { get; set; }

        public int Order { get; set; }

        public string? Status { get; set; }
    }
}
