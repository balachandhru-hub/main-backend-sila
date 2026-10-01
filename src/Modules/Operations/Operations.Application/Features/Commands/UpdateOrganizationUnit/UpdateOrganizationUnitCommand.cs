using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.UpdateOrganizationUnit
{
    /// <summary>
    /// Updates the name, kind, parent and status of a unit.
    /// </summary>
    public class UpdateOrganizationUnitCommand : IRequest<OrganizationUnitResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid UnitId { get; set; }
        public UpdateOrganizationUnitRequestDto Request { get; set; } = new();
    }
}
