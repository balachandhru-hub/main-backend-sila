namespace Operations.Domain.Dtos
{
    public class IntegrationSchemaEntityDto
    {
        public string Name { get; set; } = string.Empty;
        public string? EntitySet { get; set; }
        public List<IntegrationSchemaPropertyDto> Properties { get; set; } = new();
        public List<string> Keys { get; set; } = new();
    }
}
