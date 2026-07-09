namespace Buyer.Domain.Dto;

public class MetadataDto
{
    public Guid Id { get; set; }
    public string Key { get; set; }
    public string Type { get; set; }
    public string? Description { get; set; }
}