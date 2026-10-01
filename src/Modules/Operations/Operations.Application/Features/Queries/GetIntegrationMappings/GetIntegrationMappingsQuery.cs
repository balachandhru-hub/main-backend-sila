using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetIntegrationMappings
{
    /// <summary>
    /// Lists the field mappings of an integration configuration.
    /// </summary>
    public class GetIntegrationMappingsQuery : IRequest<List<IntegrationMappingResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
