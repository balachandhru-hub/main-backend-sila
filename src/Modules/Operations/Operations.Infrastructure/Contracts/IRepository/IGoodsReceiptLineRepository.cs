using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IGoodsReceiptLineRepository : IRepositoryBase<GoodsReceiptLine>
    {
        Task<List<GoodsReceiptLine>> GetTrackedByReceiptAsync(Guid goodsReceiptId, CancellationToken cancellationToken);
    }
}
