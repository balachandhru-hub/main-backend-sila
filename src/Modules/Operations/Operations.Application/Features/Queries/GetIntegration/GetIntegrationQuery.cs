using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetIntegration
{
    /// <summary>
    /// Returns one integration configuration of the organization.
    /// </summary>
    public class GetIntegrationQuery : IRequest<IntegrationConfigurationResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
