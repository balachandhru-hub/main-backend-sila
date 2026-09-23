using Microsoft.AspNetCore.Diagnostics;
using SharedKernel.Dto;
using Supplier.Domain.Dto;
namespace Supplier.Domain.Dto
{
    public class GetRFQByIdDto
    {
        public Guid BuyerId { get; set; }

        public string? BuyerName { get; set; }

        public Guid SupplierId { get; set; }

        /// <summary>
        /// True once the buyer has invited this supplier to contract
        /// (PUT api/v1/supplier/rfq/invite-for-contract), for this RFQ.
        /// </summary>
        public bool IsSupplierInvitedForContract { get; set; }

        public Guid? ExternalSupplierId { get; set; }

        public string? ExternalSupplierName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string DeliveryLocation { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool AddLotOption { get; set; }

        public List<AssetDto>? TechnicalSpecificationDocuments { get; set; }

        public List<AssetDto>? TermsConditionDocuments { get; set; }

        /// <summary>This supplier's own e-sign for the RFQ.</summary>
        public List<AssetDto>? ESignDocuments { get; set; }

        /// <summary>The buyer's e-sign for the RFQ.</summary>
        public List<AssetDto>? BuyerESignDocuments { get; set; }

        public bool SupplierTermsAndCondition { get; set; }

        public List<AssetDto>? SupplierTermsConditionDocuments { get; set; }

        public string BuyerTermsAndConditionAccepted { get; set; }

        public string SupplierTermsAndConditionAccepted { get; set; }
        public List<GetRFQItemDto> Items { get; set; }
        public List<GetSupplierQuotationDto> SupplierQuotation {get;set;}
         public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } 
        public List<RFQQuestionResponseDto> Questions { get; set; }
        public List<InvitedUserDto>? InvitedUsers { get; set; }

        public string Status{get;set;}

        /// <summary>
        /// Status of this supplier's contract (from the Contract table) once the RFQ is awarded
        /// and a contract exists; null until then.
        /// </summary>
        public string? ContractStatus { get; set; }

        /// <summary>
        /// Id of this supplier's contract for the RFQ; null until a contract exists.
        /// </summary>
        public Guid? ContractId { get; set; }

    }
     public class GetRFQItemDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialGroup { get; set; }
        public string? CostCenter { get; set; }
         public string? CostCenterName { get; set; }
        public List<AssetDto>? Attachments { get; set; }
        public Guid? SupplierRFQId{get;set;}
        public Guid? SupplierRFQItemId{get;set;}
        public Guid? BuyerRFQItemId {get;set;}
        public int LineNumber { get; set; }
        public bool IsAwarded { get; set; }
        public Guid? AwardedSupplierId { get; set; }

    }
     public class GetSupplierQuotationDto
    {
        
        public decimal? TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? Status { get; set; }
        public Guid? QutationId {get;set;}
        public bool IsLead { get; set; }
        public string? Rank { get; set; }


    }

         public class SupplierQuotationItemDto
    {
     public decimal QuotedPrice { get; set; }
     public Guid SupplierRFQItemId { get; set; }
     public Guid ItemQuotationId { get; set; }
      public decimal? DeliveryCharge { get; set; }
    public string? DeliveryType { get; set; }
    public Guid BuyerRFQItemId { get; set; }

    public decimal? Discount { get; set; }
    public string? DiscountType { get; set; }

    public decimal? Tax { get; set; }
    public string? TaxType { get; set; }

    public decimal QuotedAmount { get; set; }
    public decimal SubTotal { get; set; }
      public int LineNumber { get; set; }
      public string? Rank { get; set; }
      public bool IsAwarded { get; set; }

    }

    public class GetAllSupplierQuotationDto
{
    public List<SupplierQuotationBySupplierDto> Suppliers { get; set; } = new();
}

public class SupplierQuotationBySupplierDto
{
    public Guid SupplierRFQId { get; set; }

    public Guid SupplierId { get; set; }
    public string  Currency { get; set; } 

    public string? SupplierName { get; set; }

    public decimal? TotalPrice { get; set; }

    public decimal? DeliveryCharge { get; set; }

    public decimal? Tax { get; set; }

    public decimal? Discount { get; set; }

    public string? DeliveryType { get; set; }

    public string? Status { get; set; }

    public Guid? QuotationId { get; set; }
    public bool IsLead { get; set; }
    public string? Rank { get; set; }

    public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } = new();
}
 public class GetAllSupplierQuotationBySupplierIdDto
{
    public List<SupplierQuotationBySupplierIdDto> Suppliers { get; set; } = new();
}

public class SupplierQuotationBySupplierIdDto
{
    public Guid SupplierRFQId { get; set; }

    public Guid SupplierId { get; set; }

    public string? SupplierName { get; set; }

    public decimal? TotalPrice { get; set; }

    public decimal? DeliveryCharge { get; set; }

    public decimal? Tax { get; set; }

    public decimal? Discount { get; set; }

    public string? DeliveryType { get; set; }

    public string? Status { get; set; }

    public Guid? QuotationId { get; set; }
    public bool IsLead { get; set; }
    public string  Currency { get; set; } 
     
    public string? Rank { get; set; }
    public bool IsAwarded { get; set; }
    public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } = new();
}
       
    }
