using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.SupplierCatalog
{
    public class GetAllSupplierCatalogQuery
        : IRequest<List<GetSupplierCatalogDto>>
    {
        public Guid OrganizationId { get; set; }
    }
}