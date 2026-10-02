using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SupplierCatalog
{
    public class SyncSupplierCatalogCommand
        : IRequest<SupplierCatalogSyncResultDto>
    {
        public SupplierCatalogSyncRequestDto Request { get; set; }
    }
}
