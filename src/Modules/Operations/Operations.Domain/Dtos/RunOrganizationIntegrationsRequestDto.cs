using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class RunOrganizationIntegrationsRequestDto
    {
        public List<Guid> OrganizationIds { get; set; } = new();
        public IntegrationProcessType ProcessType { get; set; }
    }
}
