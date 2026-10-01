using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetDueIntegrations
{
    /// <summary>
    /// Sent by the integration scheduler worker: the active, scheduled pulls that are due.
    /// </summary>
    public class GetDueIntegrationsQuery : IRequest<List<DueIntegrationDto>>
    {

    }
}
