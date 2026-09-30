using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierErpConfiguration
{
    public class GetSupplierErpConfigurationQuery : IRequest<SupplierErpResponseDto?>
    {
        public Guid SupplierOrganizationId { get; }
        public GetSupplierErpConfigurationQuery(Guid supplierOrganizationId) => SupplierOrganizationId = supplierOrganizationId;
    }
}
