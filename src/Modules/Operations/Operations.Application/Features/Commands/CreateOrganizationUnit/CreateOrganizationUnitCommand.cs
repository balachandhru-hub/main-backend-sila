using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.CreateOrganizationUnit
{
    /// <summary>
    /// Creates a unit of the organization.
    /// </summary>
    public class CreateOrganizationUnitCommand : IRequest<OrganizationUnitResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public CreateOrganizationUnitRequestDto Request { get; set; } = new();
    }
}
