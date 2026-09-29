using System.ComponentModel.DataAnnotations;
using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class CreateContractTemplateDto
    {
        [Required]
        public long SegmentId { get; set; }

      

        [Required]
        public AssetUploadDto Attachment { get; set; }
    }
}

namespace Buyer.Domain.Dtos
{
    public class ContractTemplateResponseDto
    {
        public Guid Id { get; set; }

        public long SegmentId { get; set; }

        public string? SegmentTitle { get; set; }

        public Guid BuyerId { get; set; }

        public Guid AssetId { get; set; }

        public string? FileName { get; set; }

        public DateTime DateCreated { get; set; }
    }
}
