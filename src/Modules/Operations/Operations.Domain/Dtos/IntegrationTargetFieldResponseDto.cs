namespace Operations.Domain.Dtos
{
    public class IntegrationTargetFieldResponseDto
    {
        public string TargetField { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool Required { get; set; }
        public List<string> AllowedTransformations { get; set; } = new();
    }
}
