using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for ItemBuyerMaster.
    /// </summary>
    public class ItemBuyerMasterRepository
        : RepositoryBase<ItemBuyerMaster>,
          IItemBuyerMasterRepository
    {
        public ItemBuyerMasterRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}