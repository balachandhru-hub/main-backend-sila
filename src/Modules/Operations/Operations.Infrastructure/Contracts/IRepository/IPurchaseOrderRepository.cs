using Operations.Domain.Dtos;
using Operations.Domain.Enums;
using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IPurchaseOrderRepository : IRepositoryBase<PurchaseOrder>
    {
        Task<List<PurchaseOrder>> SearchAsync(Guid organizationId, PurchaseOrderSearchRequestDto request, CancellationToken cancellationToken);
        Task<Guid?> FindOpenIdAsync(Guid organizationId, Guid supplierId, string? poNumber, Guid? operatingUnitId, CancellationToken cancellationToken);
        Task<int> CountIntegrationRowsAsync(Guid organizationId, Guid configurationId, IntegrationDataUpdateRequestDto request, CancellationToken cancellationToken);
        Task<List<IntegrationPurchaseOrderRowDto>> ListIntegrationRowsAsync(Guid organizationId, Guid configurationId, IntegrationDataUpdateRequestDto request, CancellationToken cancellationToken);
    }
}
