using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IPurchaseOrderItemRepository : IRepositoryBase<PurchaseOrderItem>
    {
        Task<List<PurchaseOrderItem>> GetTrackedByOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken);
        Task LockForOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken);
    }
}
