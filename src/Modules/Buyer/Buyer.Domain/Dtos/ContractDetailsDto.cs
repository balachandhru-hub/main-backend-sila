using System.ComponentModel.DataAnnotations;
using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class ContractAttachmentUploadDto
    {
        [Required]
        public AssetUploadDto Asset { get; set; }
    }

    public class CreateContractDetailsDto
    {
        [Required]
        public Guid PredefinedContractId { get; set; }

        [Required]
        public ContractAttachmentUploadDto Attachment { get; set; }
    }
}

namespace Buyer.Domain.Dtos
{
    public class ContractAttachmentDetailsDto
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string? FileName { get; set; }
    }

    public class ContractDetailsResponseDto
    {
        public Guid Id { get; set; }
        public Guid PredefinedContractId { get; set; }
        public string? ContractNumber { get; set; }
        public string? ContractName { get; set; }
        public Guid RFQId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? ContractStatus { get; set; }
        public DateTime DateCreated { get; set; }
        public List<ContractAttachmentDetailsDto> Attachments { get; set; } = new();
    }
}
