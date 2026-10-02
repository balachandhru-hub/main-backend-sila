using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UpdateIntegration
{
    /// <summary>
    /// Updates an integration configuration. An active configuration stays active; any other goes back to DRAFT and must be tested again.
    /// </summary>
    public class UpdateIntegrationCommand : IRequest<IntegrationConfigurationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }

        /// <summary>Buyer or Supplier, from the caller's token.</summary>
        public string OrganizationType { get; set; } = string.Empty;
        public Guid ConfigurationId { get; set; }
        public IntegrationConfigurationInputDto Request { get; set; } = new();
    }
}
