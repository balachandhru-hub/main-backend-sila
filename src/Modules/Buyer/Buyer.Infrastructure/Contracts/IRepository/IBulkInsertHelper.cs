using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IBulkInsertHelper
    {
        Task BulkInsertOrUpdateAsync(List<ItemBuyerMaster> entities);
    }
}