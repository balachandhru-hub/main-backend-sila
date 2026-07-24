using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class UpdateSupplierCatalogCommand
        : IRequest<bool>
    {
        public Guid OrganizationId { get; set; }

        public UpdateSupplierCatalogDto Catalog { get; set; }
    }
}