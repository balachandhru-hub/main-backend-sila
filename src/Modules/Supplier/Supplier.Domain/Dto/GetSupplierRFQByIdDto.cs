using SharedKernel.Dto;
using Supplier.Domain.Dto;
namespace Supplier.Domain.Dto
{
    public class GetRFQByIdDto
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string DeliveryLocation { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool AddLotOption { get; set; }

        public List<AssetDto>? TechnicalSpecificationDocuments { get; set; }

        public List<AssetDto>? TermsConditionDocuments { get; set; }
        public List<GetRFQItemDto> Items { get; set; }
        public List<GetSupplierQuotationDto> SupplierQuotation {get;set;}
         public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } 
        public List<RFQQuestionResponseDto> Questions { get; set; }
      
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
        public Guid? SupplierRFQId{get;set;}
        public Guid? SupplierRFQItemId{get;set;}
        public Guid? BuyerRFQItemId {get;set;}
       
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

       
    }

         public class SupplierQuotationItemDto
    {
     public decimal QuotedPrice { get; set; }
     public Guid? ItemQutationId {get;set;}

    }

     public class GetAllSupplierQuotationDto
    {
        
        public decimal? TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? Status { get; set; }
        public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } 
        public Guid? QutationId {get;set;}

       
    }
}