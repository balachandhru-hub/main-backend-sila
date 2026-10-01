using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UpdateExtractionAgent
{
    /// <summary>
    /// Updates an external extraction agent of the organization.
    /// </summary>
    public class UpdateExtractionAgentCommand : IRequest<ExtractionAgentConfigResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid AgentId { get; set; }
        public UpsertExtractionAgentConfigRequestDto Request { get; set; } = new();
    }
}
