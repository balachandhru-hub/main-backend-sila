using System.ComponentModel.DataAnnotations;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class UpsertExtractionAgentConfigRequestDto
    {
        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(50)]
        public string DocumentType { get; set; } = "INVOICE";
        [Required, StringLength(100)]
        public string ProviderType { get; set; } = "CUSTOM_REST";
        [Url, StringLength(2000)]
        public string? EndpointUrl { get; set; }
        public ExtractionAuthenticationType AuthenticationType { get; set; } = ExtractionAuthenticationType.NONE;
        [StringLength(500)]
        public string? CredentialReference { get; set; }
        [Range(1, 100000)]
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
