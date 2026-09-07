using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetOrganizationUserRFQ
{
    public class GetOrganizationUserRFQQuery : IRequest<List<UserListDto>>
    {
        public Guid? OrganizationId { get; set; }
    }
}