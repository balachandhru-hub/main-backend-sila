using SharedKernel.Dto;
namespace Supplier.Domain.Dto
{
    public class BuyerCatalogDto
    {
        public Guid SupplierId { get; set; }

        public Guid? CatalogId { get; set; }

        public string SupplierName { get; set; }

        public string? CatalogName { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }
        public string Currency { get; set; }

        public string? UnitOfMeasure { get; set; }

        public long? Segment { get; set; }
         public long? Family { get; set; }
        public long? Commodity { get; set; }
        public long? Class { get; set; }
       public List<AssetDto> Asset { get; set; }= new();

   
    }
}