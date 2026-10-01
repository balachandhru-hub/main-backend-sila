using MediatR;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;

namespace Operations.Application.Features.Commands.RunIntegration
{
    /// <summary>
    /// Pulls purchase orders or suppliers from the configured API. Also sent by the integration scheduler worker.
    /// </summary>
    public class RunIntegrationCommand : IRequest<IntegrationExecutionResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationExecutionTrigger Trigger { get; set; } = IntegrationExecutionTrigger.MANUAL;
        public bool FullSync { get; set; }
    }
}
