namespace MasterData.Domain.Dto;

public class UnitDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}
