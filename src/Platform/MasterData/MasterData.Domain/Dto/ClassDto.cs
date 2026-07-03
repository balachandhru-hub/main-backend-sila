public class ClassDto
{
    public long? Class { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<CommodityDto> Commodity { get; set; } = new();
}