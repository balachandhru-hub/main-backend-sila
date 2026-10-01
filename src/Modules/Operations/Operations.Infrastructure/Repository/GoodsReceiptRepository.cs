using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class GoodsReceiptRepository : RepositoryBase<GoodsReceipt>, IGoodsReceiptRepository
    {
        public GoodsReceiptRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
