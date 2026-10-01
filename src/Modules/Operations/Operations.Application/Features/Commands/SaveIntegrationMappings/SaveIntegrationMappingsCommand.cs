using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.SaveIntegrationMappings
{
    /// <summary>
    /// Replaces the field mappings of an integration configuration.
    /// </summary>
    public class SaveIntegrationMappingsCommand : IRequest<List<IntegrationMappingResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public List<FieldMappingInputDto> Mappings { get; set; } = new();
    }
}
