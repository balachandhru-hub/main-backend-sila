using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class PurchaseOrderItemRepository : RepositoryBase<PurchaseOrderItem>, IPurchaseOrderItemRepository
    {
        public PurchaseOrderItemRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
