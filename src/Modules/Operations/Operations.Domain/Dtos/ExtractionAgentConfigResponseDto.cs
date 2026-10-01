using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class ExtractionAgentConfigResponseDto
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public string? EndpointUrl { get; set; }
        public ExtractionAuthenticationType AuthenticationType { get; set; }
        public string? CredentialMask { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
