using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetIntegrationExecutions
{
    /// <summary>
    /// Lists the latest runs of the organization's integrations, optionally of one configuration.
    /// </summary>
    public class GetIntegrationExecutionsQuery : IRequest<List<IntegrationExecutionResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ConfigurationId { get; set; }
    }
}
