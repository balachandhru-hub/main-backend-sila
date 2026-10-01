using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.TestIntegration
{
    /// <summary>
    /// Calls the configured API once (one record) and records the result. A failed call is a result, not an error.
    /// </summary>
    public class TestIntegrationCommand : IRequest<IntegrationTestResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
