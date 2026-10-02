using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class UpdateSupplierCatalogStockCommand
        : IRequest<bool>
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public UpdateSupplierCatalogStockDto Stock { get; set; }
    }
}
