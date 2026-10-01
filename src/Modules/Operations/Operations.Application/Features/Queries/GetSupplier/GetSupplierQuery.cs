using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetSupplier
{
    /// <summary>
    /// Returns one supplier of the organization.
    /// </summary>
    public class GetSupplierQuery : IRequest<SupplierResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid SupplierId { get; set; }
    }
}
