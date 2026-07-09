using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Entities
{
    public class BuyerCategory : BaseModel
{
    [Key]
    public Guid Id { get; set; }

    public Guid BuyerId { get; set; }

    // UNSPSC Hierarchy
    public long Segment { get; set; }

    public string SegmentTitle { get; set; }

    public long? Family { get; set; }

    public string? FamilyTitle { get; set; }

    public long? Class { get; set; }

    public string? ClassTitle { get; set; }

    public long? Commodity { get; set; }

    public string? CommodityTitle { get; set; }

    public bool IsActive { get; set; }
    public BuyerCategory()
    {
    }
}
}