using System.ComponentModel.DataAnnotations;
using Buyer.Domain.Dtos;
using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class CreatePredefinedContractDto
    {
        [Required]
        public string ContractName { get; set; }

        [Required]
        public Guid RFQId { get; set; }

        [Required]
        public Guid? SupplierId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public List<AssetUploadDto>? Attachments { get; set; }
    }

    public class PredefinedContractAttachmentDto
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }

        public string? Type { get; set; }

        public string? FileName { get; set; }

        public string Title { get; set; }

        public Guid? UnspscId { get; set; }

        public long? SegmentId { get; set; }

        public string? SegmentTitle { get; set; }
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

        public List<PredefinedContractApprovalFlowDto> ApprovalFlows { get; set; } = new();

        public List<PredefinedContractApprovalUserDto> ApprovalUsers { get; set; } = new();
    }

    public class PredefinedContractApprovalUserDto
    {
        public Guid UserId { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public int Order { get; set; }

        public string? Status { get; set; }
    }
}
