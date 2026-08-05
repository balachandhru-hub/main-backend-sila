using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetOrganizationUser
{
    public class GetOrganizationUserQuery : IRequest<List<UserListDto>>
    {
        public Guid? OrganizationId { get; set; }

        public string LoggedInRole { get; set; }
    }
}