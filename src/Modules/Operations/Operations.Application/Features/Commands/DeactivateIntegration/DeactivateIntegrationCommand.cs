using MediatR;

namespace Operations.Application.Features.Commands.DeactivateIntegration
{
    /// <summary>
    /// Deactivates an integration configuration: no scheduled pull and no goods receipt post uses it.
    /// </summary>
    public class DeactivateIntegrationCommand : IRequest<bool>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
