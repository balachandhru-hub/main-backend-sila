using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.RunOrganizationIntegrations
{
    /// <summary>
    /// Runs the active APIs of one type of several organizations now, for example the stock APIs of the suppliers a buyer is about to order from.
    /// </summary>
    public class RunOrganizationIntegrationsCommand : IRequest<RunOrganizationIntegrationsResultDto>
    {
        public RunOrganizationIntegrationsRequestDto Request { get; set; } = new();
    }
}
