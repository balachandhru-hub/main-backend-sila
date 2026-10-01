using MediatR;

namespace Operations.Application.Features.Commands.DeleteOrganizationUnit
{
    /// <summary>
    /// Deactivates a unit. A unit that still has active sub-units cannot be deleted.
    /// </summary>
    public class DeleteOrganizationUnitCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid UnitId { get; set; }
    }
}
