using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationMappingResponseDto
    {
        public Guid Id { get; set; }
        public Guid ConfigurationId { get; set; }
        public string SourceField { get; set; } = string.Empty;
        public string TargetField { get; set; } = string.Empty;
        public string? Transformation { get; set; }
        public IntegrationNullPolicy NullPolicy { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsValidated { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
