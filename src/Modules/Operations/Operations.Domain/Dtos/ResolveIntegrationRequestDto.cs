using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class ResolveIntegrationRequestDto
    {
        public Guid OrganizationId { get; set; }
        public IntegrationProcessType ProcessType { get; set; }

        /// <summary>Preferred entity (company code). The organization-wide configuration is used when it has none of its own.</summary>
        public string? EntityCode { get; set; }
    }
}
