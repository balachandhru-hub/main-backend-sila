using MasterData.Domain.Entities;

namespace MasterData.Application.Contracts.IRepository;

public interface IBulkInsertHelper
{
    Task BulkInsertOrUpdateAsync(List<UnspscCategory> entities);
}