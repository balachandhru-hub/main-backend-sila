using MediatR;

namespace Identity.Application.Features.Queries.Permissions
{
    public class GetPermissionsQuery : IRequest<List<string>>
    {
        public Guid RoleId { get; set; }
    }
}
