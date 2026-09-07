using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetUsersByIds
{
    public class GetUsersByIdsQuery : IRequest<List<UserListDto>>
    {
        public List<Guid> UserIds { get; set; } = new();
    }
}
