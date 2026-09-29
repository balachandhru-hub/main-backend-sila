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

        public List<AssetDto>? ESignDocuments { get; set; }

        public List<RFQQuestionDto> Questions { get; set; }

        public List<GetRFQItemDto> Items { get; set; }

        public List<SupplierNameDto> SupplierIds { get; set; } = new();

        public List<ExternalSupplierNameDto> ExternalSupplierIds { get; set; } = new();

        public Guid RFQVerificationTemplateId { get; set; }
       public List<SupplierQuotationBySupplierDto> SupplierQuotation { get; set; } = new();
        public SupplierRFQAnswerDto? SupplierAnswers { get; set; }
        public List<InvitedUserDto>? InvitedUsers { get; set; }
        public List<RFQTermsConditionDto> SupplierTermsConditions { get; set; } = new();
        public List<RFQESignDto> SupplierESigns { get; set; } = new();
        public List<SupplierTermsAndConditionAcceptedDto> SupplierTermsAndConditionAccepted { get; set; } = new();
        public List<BuyerTermsAndConditionStatusDto> BuyerTermsAndConditionStatuses { get; set; } = new();
        public string Status {get;set;}

        /// <summary>
        /// Contracts created for this RFQ (one per supplier), empty until a contract exists.
        /// </summary>
        public List<RFQPredefinedContractDto> Contracts { get; set; } = new();

        /// <summary>
        /// Template documents for the RFQ's segment.
        /// </summary>
        public List<AssetDto> ContractTemplateDocuments { get; set; } = new();
        public long? SegmentId { get; set; }
        public string? SegmentTitel { get; set; }
        public long? FamilyId { get; set; }
    }

    public class RFQPredefinedContractDto
    {
        public Guid ContractId { get; set; }

        public string? ContractNumber { get; set; }

        public Guid SupplierId { get; set; }
    }

    public class GetRFQItemDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialGroup { get; set; }
        public string CostCenter { get; set; }
        public List<AssetDto>? Attachments { get; set; }
        public bool IsAwarded { get; set; }

    }


}