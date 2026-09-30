using MediatR;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class DeleteSupplierCatalogCommand
        : IRequest<bool>
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }
    }
}