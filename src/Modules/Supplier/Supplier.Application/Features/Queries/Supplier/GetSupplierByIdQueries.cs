using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetSupplierByIdQuery : IRequest<OrganizationDto>
    {
        public Guid SupplierId { get; set; }
    }
}