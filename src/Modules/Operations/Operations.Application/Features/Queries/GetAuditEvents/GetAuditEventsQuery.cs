using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetAuditEvents
{
    /// <summary>
    /// Lists the latest audit events of the organization, optionally for one record.
    /// </summary>
    public class GetAuditEventsQuery : IRequest<List<AuditEventResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public string? EntityType { get; set; }
        public Guid? EntityId { get; set; }
        public int Limit { get; set; } = 200;
    }
}
