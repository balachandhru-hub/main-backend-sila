using MediatR;
using Supplier.Application.Services;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.SaveSupplierErpConfiguration
{
    public class SaveSupplierErpConfigurationCommandHandler : IRequestHandler<SaveSupplierErpConfigurationCommand, Guid>
    {
        private readonly ISupplierPurchaseOrderService _service;

        public SaveSupplierErpConfigurationCommandHandler(IRepositoryWrapper repository)
            => _service = new SupplierPurchaseOrderService(repository);

        public Task<Guid> Handle(SaveSupplierErpConfigurationCommand request, CancellationToken cancellationToken) =>
            _service.SaveConfigurationAsync(request.SupplierOrganizationId, request.Request, cancellationToken);
    }
}
