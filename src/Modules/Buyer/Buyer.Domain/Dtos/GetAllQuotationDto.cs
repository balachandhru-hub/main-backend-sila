  
  namespace Buyer.Domain.Dto
{
    public class GetAllSupplierQuotationDto
    {
        
        public decimal? TotalPrice { get; set; }

        public decimal? DeliveryCharge { get; set; }

        public decimal? Tax { get; set; }

        public decimal? Discount { get; set; }

        public string? DeliveryType { get; set; }

        public string? Status { get; set; }
        public List<SupplierQuotationItemDto> SupplierQuotationItems { get; set; } 
        public Guid? QuotationId {get;set;}

       
    }
        public class SupplierQuotationItemDto
    {
     public decimal QuotedPrice { get; set; }
      public Guid? ItemQuotationId {get;set;}
    }
}