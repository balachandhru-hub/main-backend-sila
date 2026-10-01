using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class StockBalanceRepository : RepositoryBase<StockBalance>, IStockBalanceRepository
    {
        public StockBalanceRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
