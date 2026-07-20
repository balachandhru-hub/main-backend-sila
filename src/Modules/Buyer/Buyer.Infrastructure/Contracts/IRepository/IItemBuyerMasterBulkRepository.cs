using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IItemBuyerMasterBulkRepository
    {
        Task BulkInsertOrUpdateAsync(List<ItemBuyerMaster> entities);
    }
}