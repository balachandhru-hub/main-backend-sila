namespace Operations.Domain.Dtos
{
    public class IntegrationSchemaResponseDto
    {
        public Guid ConfigurationId { get; set; }
        public string MetadataUrl { get; set; } = string.Empty;
        public DateTime DiscoveredAt { get; set; }
        public List<IntegrationSchemaEntityDto> Entities { get; set; } = new();
    }
}
