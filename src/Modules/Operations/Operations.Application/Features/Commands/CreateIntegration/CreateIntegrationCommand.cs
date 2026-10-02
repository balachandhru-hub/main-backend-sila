using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.CreateIntegration
{
    /// <summary>
    /// Creates an integration configuration (status DRAFT until it is tested and activated).
    /// </summary>
    public class CreateIntegrationCommand : IRequest<IntegrationConfigurationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Buyer or Supplier, from the caller's token.</summary>
        public string OrganizationType { get; set; } = string.Empty;
        public IntegrationConfigurationInputDto Request { get; set; } = new();
    }
}
