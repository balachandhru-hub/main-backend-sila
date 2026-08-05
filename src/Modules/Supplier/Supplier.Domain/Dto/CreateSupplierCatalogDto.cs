using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class CreateSupplierCatalogDto
    {
        public string CatalogName { get; set; }

        public string Description { get; set; }

        public decimal? Price { get; set; }

        public string UnitOfMeasure { get; set; }
        public string CatalogType { get; set; }   // Catalog / NonCatalog
        public long? Segment { get; set; }
        public string? SegmentTitle { get; set; }
        public long? Family { get; set; }
        public string? FamilyTitle { get; set; }
        public long? Commodity { get; set; }
        public string? CommodityTitle { get; set; }
        public long? Class { get; set; }
        public string? ClassTitle { get; set; }
        public bool IsPunchOut { get; set; }

        public string? PunchOutUrl { get; set; }

        public List<AssetUploadDto> Assets { get; set; } = new();
    }
}