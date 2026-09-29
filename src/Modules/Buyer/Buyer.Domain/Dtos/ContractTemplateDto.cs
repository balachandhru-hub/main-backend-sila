using System.ComponentModel.DataAnnotations;
using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class CreateContractTemplateDto
    {
        [Required]
        public long SegmentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; }

        [Required]
        public AssetUploadDto Attachment { get; set; }
    }

    // All fields are optional; only the ones supplied are updated.
    public class UpdateContractTemplateDto
    {
        public long? SegmentId { get; set; }

        [MaxLength(200)]
        public string? TemplateName { get; set; }

        public AssetUploadDto? Attachment { get; set; }
    }
}

namespace Buyer.Domain.Dtos
{
    public class ContractTemplateResponseDto
    {
        public Guid Id { get; set; }

        public long SegmentId { get; set; }

        public string? SegmentTitle { get; set; }

        public string? TemplateName { get; set; }

        public Guid BuyerId { get; set; }

        public Guid AssetId { get; set; }

        public string? FileName { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
