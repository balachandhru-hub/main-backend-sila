using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.TestExtractionAgent
{
    /// <summary>
    /// Checks that the endpoint of an extraction agent is a valid HTTP(S) address.
    /// </summary>
    public class TestExtractionAgentCommand : IRequest<ExtractionAgentTestResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid AgentId { get; set; }
    }
}
