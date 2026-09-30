using MediatR;
using Supplier.Application.Services;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.SupplierErp
{
    public class CreateSupplierPurchaseOrderCommand : IRequest<SupplierPurchaseOrderResponseDto>
    {
        public SupplierPurchaseOrderRequestDto Request { get; }
        public CreateSupplierPurchaseOrderCommand(SupplierPurchaseOrderRequestDto request) => Request = request;
    }

    public class GetSupplierErpConfigurationQuery : IRequest<SupplierErpResponseDto?>
    {
        public Guid SupplierOrganizationId { get; }
        public GetSupplierErpConfigurationQuery(Guid supplierOrganizationId) => SupplierOrganizationId = supplierOrganizationId;
    }

    public class SaveSupplierErpConfigurationCommand : IRequest<Guid>
    {
        public Guid SupplierOrganizationId { get; }
        public SupplierErpWriteDto Request { get; }
        public SaveSupplierErpConfigurationCommand(Guid supplierOrganizationId, SupplierErpWriteDto request)
        {
            SupplierOrganizationId = supplierOrganizationId;
            Request = request;
        }
    }

    public class CreateSupplierPurchaseOrderCommandHandler : IRequestHandler<CreateSupplierPurchaseOrderCommand, SupplierPurchaseOrderResponseDto>
    {
        private readonly ISupplierPurchaseOrderService _service;
        public CreateSupplierPurchaseOrderCommandHandler(ISupplierPurchaseOrderService service) => _service = service;
        public Task<SupplierPurchaseOrderResponseDto> Handle(CreateSupplierPurchaseOrderCommand request, CancellationToken cancellationToken) =>
            _service.CreateAsync(request.Request, cancellationToken);
    }

    public class GetSupplierErpConfigurationQueryHandler : IRequestHandler<GetSupplierErpConfigurationQuery, SupplierErpResponseDto?>
    {
        private readonly ISupplierPurchaseOrderService _service;
        public GetSupplierErpConfigurationQueryHandler(ISupplierPurchaseOrderService service) => _service = service;
        public Task<SupplierErpResponseDto?> Handle(GetSupplierErpConfigurationQuery request, CancellationToken cancellationToken) =>
            _service.GetConfigurationAsync(request.SupplierOrganizationId, cancellationToken);
    }

    public class SaveSupplierErpConfigurationCommandHandler : IRequestHandler<SaveSupplierErpConfigurationCommand, Guid>
    {
        private readonly ISupplierPurchaseOrderService _service;
        public SaveSupplierErpConfigurationCommandHandler(ISupplierPurchaseOrderService service) => _service = service;
        public Task<Guid> Handle(SaveSupplierErpConfigurationCommand request, CancellationToken cancellationToken) =>
            _service.SaveConfigurationAsync(request.SupplierOrganizationId, request.Request, cancellationToken);
    }
}
