namespace Operations.Domain.Dtos
{
    public class IntegrationImportPreviewResponseDto
    {
        public Guid ConfigurationId { get; set; }
        public string Kind { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public List<string> Columns { get; set; } = new();
        public List<IntegrationImportRowResponseDto> Rows { get; set; } = new();
    }
}
