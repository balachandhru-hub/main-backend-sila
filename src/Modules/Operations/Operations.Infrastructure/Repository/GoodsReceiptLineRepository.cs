using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class GoodsReceiptLineRepository : RepositoryBase<GoodsReceiptLine>, IGoodsReceiptLineRepository
    {
        public GoodsReceiptLineRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<GoodsReceiptLine>> GetTrackedByReceiptAsync(Guid goodsReceiptId, CancellationToken cancellationToken)
        {
            return RepositoryContext.GoodsReceiptLine
                .Where(x => x.GoodsReceiptId == goodsReceiptId)
                .ToListAsync(cancellationToken);
        }
    }
}
