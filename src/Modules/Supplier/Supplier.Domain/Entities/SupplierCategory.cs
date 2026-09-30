using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Supplier.Domain.Entities
{
    public class SupplierCategory : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    // UNSPSC Hierarchy
    public long Segment { get; set; }

    public string SegmentTitle { get; set; }

    public long? Family { get; set; }

    public string? FamilyTitle { get; set; }

    public long? Class { get; set; }

    public string? ClassTitle { get; set; }

    public long? Commodity { get; set; }

    public string? CommodityTitle { get; set; }
    public SupplierCategory()
    {
    }
}
}