using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetStorageConnections
{
    /// <summary>
    /// Lists the external document storage connections of the organization.
    /// </summary>
    public class GetStorageConnectionsQuery : IRequest<List<StorageConnectionResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
