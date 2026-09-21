using System.ComponentModel.DataAnnotations;
using Buyer.Domain.Dtos;
using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class CreateContractDto
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

    public class ContractAttachmentDto
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }

        public string? Type { get; set; }

        public string? FileName { get; set; }
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
