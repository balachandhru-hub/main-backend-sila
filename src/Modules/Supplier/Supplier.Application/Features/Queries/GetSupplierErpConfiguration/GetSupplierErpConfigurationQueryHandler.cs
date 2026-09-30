using MediatR;
using Supplier.Application.Services;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetSupplierErpConfiguration
{
    public class GetSupplierErpConfigurationQueryHandler : IRequestHandler<GetSupplierErpConfigurationQuery, SupplierErpResponseDto?>
    {
        private readonly ISupplierPurchaseOrderService _service;

        public GetSupplierErpConfigurationQueryHandler(IRepositoryWrapper repository)
            => _service = new SupplierPurchaseOrderService(repository);

        public Task<SupplierErpResponseDto?> Handle(GetSupplierErpConfigurationQuery request, CancellationToken cancellationToken) =>
            _service.GetConfigurationAsync(request.SupplierOrganizationId, cancellationToken);
    }
}
