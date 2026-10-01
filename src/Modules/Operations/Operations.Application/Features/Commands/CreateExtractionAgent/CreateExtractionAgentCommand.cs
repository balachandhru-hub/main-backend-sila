using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.CreateExtractionAgent
{
    /// <summary>
    /// Creates an external extraction agent for the organization.
    /// </summary>
    public class CreateExtractionAgentCommand : IRequest<ExtractionAgentConfigResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public UpsertExtractionAgentConfigRequestDto Request { get; set; } = new();
    }
}
