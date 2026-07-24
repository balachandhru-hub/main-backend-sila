using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class CreateSupplierCatalogCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }

        public CreateSupplierCatalogDto Catalog { get; set; }
    }
}