namespace MasterData.Domain.Dto
{
    public class GetMetadataByKeysRequestDto
    {
        public string Type { get; set; } = string.Empty;

        public List<string> Keys { get; set; } = new();
    }
}