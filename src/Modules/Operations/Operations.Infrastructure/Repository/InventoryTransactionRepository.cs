using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class InventoryTransactionRepository : RepositoryBase<InventoryTransaction>, IInventoryTransactionRepository
    {
        public InventoryTransactionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
