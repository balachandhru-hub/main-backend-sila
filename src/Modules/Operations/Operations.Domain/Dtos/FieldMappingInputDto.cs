using System.ComponentModel.DataAnnotations;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class FieldMappingInputDto
    {
        [Required] public string SourceField { get; set; } = string.Empty;
        [Required] public string TargetField { get; set; } = string.Empty;
        public string? Transformation { get; set; }
        public IntegrationNullPolicy NullPolicy { get; set; } = IntegrationNullPolicy.IGNORE_NULL;
        public string? DefaultValue { get; set; }
    }
}
