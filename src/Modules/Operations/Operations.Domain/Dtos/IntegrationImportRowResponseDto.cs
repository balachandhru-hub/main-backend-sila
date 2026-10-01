namespace Operations.Domain.Dtos
{
    public class IntegrationImportRowResponseDto
    {
        public int RowNumber { get; set; }
        public bool IsValid { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}
