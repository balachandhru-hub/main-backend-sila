using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetSuppliers
{
    /// <summary>
    /// Searches the supplier master of the organization.
    /// </summary>
    public class GetSuppliersQuery : IRequest<List<SupplierResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public SupplierSearchRequestDto Request { get; set; } = new();
    }
}
