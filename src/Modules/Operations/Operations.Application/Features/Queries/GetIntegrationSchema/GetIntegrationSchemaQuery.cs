using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetIntegrationSchema
{
    /// <summary>
    /// Returns the latest schema snapshot of an integration configuration.
    /// </summary>
    public class GetIntegrationSchemaQuery : IRequest<IntegrationSchemaResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
