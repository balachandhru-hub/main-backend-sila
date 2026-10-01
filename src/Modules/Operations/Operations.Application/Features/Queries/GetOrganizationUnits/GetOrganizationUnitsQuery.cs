using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetOrganizationUnits
{
    /// <summary>
    /// Lists the units (property, hotel, outlet, kitchen, store) of the organization.
    /// </summary>
    public class GetOrganizationUnitsQuery : IRequest<List<OrganizationUnitResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
