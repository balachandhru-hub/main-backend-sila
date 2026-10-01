using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SaveSupplierErpConfiguration
{
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
}
