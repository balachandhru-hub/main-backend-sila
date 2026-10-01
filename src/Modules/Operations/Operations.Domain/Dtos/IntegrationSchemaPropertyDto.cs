namespace Operations.Domain.Dtos
{
    public class IntegrationSchemaPropertyDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool Nullable { get; set; }
    }
}
