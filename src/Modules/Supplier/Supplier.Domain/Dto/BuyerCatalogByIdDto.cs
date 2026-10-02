using SharedKernel.Dto;
namespace Supplier.Domain.Dto
{
    public class BuyerCatalogByIdDto
    {
        public Guid SupplierId { get; set; }

        public Guid? CatalogId { get; set; }

        public string SupplierName { get; set; }

        public string? CatalogName { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }
        public string Currency { get; set; }

        public string? UnitOfMeasure { get; set; }
        public string? Sku { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal? DiscountPercent { get; set; }

        public long? Segment { get; set; }
        public string? SegmentTitle { get; set; }
         public long? Family { get; set; }
         public string? FamilyTitle { get; set; }
        public long? Commodity { get; set; }
        public string? CommodityTitle { get; set; }
        public long? Class { get; set; }
        public string? ClassTitle { get; set; }
        public string? CatalogType { get; set; }
       public List<AssetDto> Asset { get; set; }= new();

   
    }
}