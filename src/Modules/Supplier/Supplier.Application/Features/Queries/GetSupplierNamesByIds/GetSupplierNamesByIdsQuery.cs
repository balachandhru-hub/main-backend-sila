using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierNamesByIds
{
    public class GetSupplierNamesByIdsQuery : IRequest<List<SupplierNameDto>>
    {
        public List<Guid> SupplierIds { get; set; } = new();
    }
}
