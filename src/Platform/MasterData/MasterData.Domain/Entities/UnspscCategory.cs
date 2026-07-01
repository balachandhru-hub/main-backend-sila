using MasterData.Domain.Common;

namespace MasterData.Domain.Entities;

public class UnspscCategory : BaseEntity
{
    public string? Version { get; set; }

    public int Key { get; set; }

    public long Segment { get; set; }

    public string SegmentTitle { get; set; } = string.Empty;

    public string SegmentDefinition { get; set; } = string.Empty;

    public long? Family { get; set; }

    public string? FamilyTitle { get; set; }

    public string? FamilyDefinition { get; set; }

    public long? Class { get; set; }

    public string? ClassTitle { get; set; }

    public string? ClassDefinition { get; set; }

    public long? Commodity { get; set; }

    public string? CommodityTitle { get; set; }

    public string? CommodityDefinition { get; set; }

    public string? Synonym { get; set; }

    public string? Acronym { get; set; }
}