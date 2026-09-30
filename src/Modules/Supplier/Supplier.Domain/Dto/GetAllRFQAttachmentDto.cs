using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class GetRFQAttachmentsDto
    {
        public List<AssetDto> TechnicalSpecificationDocuments { get; set; } 

        public List<AssetDto> TermsConditionDocuments { get; set; }

        /// <summary>
        /// The buyer's own e-sign for this RFQ (one per RFQ, not per supplier).
        /// </summary>
        public List<AssetDto> ESignDocuments { get; set; } = new();

        /// <summary>
        /// The buyer's contract template documents for the RFQ's segment.
        /// </summary>
        public List<AssetDto> ContractTemplateDocuments { get; set; } = new();
        public List<RFQItemAttachmentDto> ItemAttachments { get; set; }
    }
     public class RFQItemAttachmentDto
    {
        public Guid RFQItemId { get; set; }

        public List<AssetDto> Attachments { get; set; } 
    }
}