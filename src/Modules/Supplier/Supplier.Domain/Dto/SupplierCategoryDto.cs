namespace Supplier.Domain.Dto
{
    public class SupplierCategoryDto
    {
        public long Segment { get; set; }

        public string SegmentTitle { get; set; }

        public long? Family { get; set; }

        public string? FamilyTitle { get; set; }

        public long? Class { get; set; }

        public string? ClassTitle { get; set; }

        public long? Commodity { get; set; }

        public string? CommodityTitle { get; set; }
    }
}
