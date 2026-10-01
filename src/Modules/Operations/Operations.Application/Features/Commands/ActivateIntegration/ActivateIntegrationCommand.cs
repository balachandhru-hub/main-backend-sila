using MediatR;

namespace Operations.Application.Features.Commands.ActivateIntegration
{
    /// <summary>
    /// Activates an integration configuration. It must have been tested successfully.
    /// </summary>
    public class ActivateIntegrationCommand : IRequest<bool>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
