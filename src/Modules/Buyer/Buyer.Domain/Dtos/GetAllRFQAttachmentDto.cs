using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class GetRFQAttachmentsDto
    {
        public List<AssetDto> TechnicalSpecificationDocuments { get; set; } 

        public List<AssetDto> TermsConditionDocuments { get; set; } 
        public List<RFQItemAttachmentDto> ItemAttachments { get; set; }
    }
     public class RFQItemAttachmentDto
    {
        public Guid RFQItemId { get; set; }

        public List<AssetDto> Attachments { get; set; } 
    }
}