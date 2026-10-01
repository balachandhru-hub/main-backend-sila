using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetExtractionAgents
{
    /// <summary>
    /// Lists the external extraction agents available to the organization.
    /// </summary>
    public class GetExtractionAgentsQuery : IRequest<List<ExtractionAgentConfigResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
