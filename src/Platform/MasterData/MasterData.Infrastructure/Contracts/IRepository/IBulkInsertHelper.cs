using MasterData.Domain.Entities;

namespace MasterData.Infrastructure.Contracts.IRepository;

public interface IBulkInsertHelper
{
    Task BulkInsertOrUpdateAsync(List<UnspscCategory> entities);
}