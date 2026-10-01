using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetOutletUsers
{
    public class GetOutletUsersQuery : IRequest<List<OutletUserMappingDto>>
    {
        public Guid OrganizationId { get; set; }
    }
}
