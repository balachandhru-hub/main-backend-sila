using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.SupplierCatalog
{
    public class GetSupplierCatalogByIdQuery : IRequest<List<SupplierCatalogByIdDto>>
    {
        public Guid? CatalogId { get; set; }
    }
}
