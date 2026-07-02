public class SegmentDto
{
    public long Segment { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<FamilyDto> Family { get; set; } = new();
}