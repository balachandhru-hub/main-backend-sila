using SharedKernel.Dto;
using Buyer.Domain.Dto;
namespace Buyer.Domain.Dtos
{
    public class GetRFQByIdDto
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string Department { get; set; }

        public string Region { get; set; }

        public string Currency { get; set; }

        public string DeliveryLocation { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime DeliveryTargetDate { get; set; }

        public decimal Budget { get; set; }

        public bool AddLotOption { get; set; }

        public List<AssetDto>? TechnicalSpecificationDocuments { get; set; }

        public List<AssetDto>? TermsConditionDocuments { get; set; }

        public List<RFQQuestionDto> Questions { get; set; }

        public List<GetRFQItemDto> Items { get; set; }

        public List<Guid> SupplierIds { get; set; }

        public Guid RFQVerificationTemplateId { get; set; }
    }
     public class GetRFQItemDto
    {
        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialGroup { get; set; }
        public string CostCenter { get; set; }
        public List<AssetDto>? Attachments { get; set; }
       
    }

      
}