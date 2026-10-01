using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetIntegrationDataUpdate
{
    /// <summary>
    /// Returns the purchase orders or suppliers imported for an integration (paged) with the totals of the last sync.
    /// </summary>
    public class GetIntegrationDataUpdateQuery : IRequest<IntegrationDataUpdateResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationDataUpdateRequestDto Request { get; set; } = new();
    }
}
