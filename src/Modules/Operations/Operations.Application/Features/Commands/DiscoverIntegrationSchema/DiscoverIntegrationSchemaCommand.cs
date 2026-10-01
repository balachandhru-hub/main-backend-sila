using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.DiscoverIntegrationSchema
{
    /// <summary>
    /// Reads the OData $metadata of the configured service and stores it as a schema snapshot.
    /// </summary>
    public class DiscoverIntegrationSchemaCommand : IRequest<IntegrationSchemaResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
