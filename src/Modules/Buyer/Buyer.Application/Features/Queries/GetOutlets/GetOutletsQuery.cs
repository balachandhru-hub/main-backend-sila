using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetOutlets
{
    public class GetOutletsQuery : IRequest<List<OutletResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
