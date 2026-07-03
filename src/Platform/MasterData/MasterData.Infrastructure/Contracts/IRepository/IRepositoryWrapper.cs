using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Infrastructure.Contracts.IRepository;

public interface IRepositoryWrapper
{
    IUnspscRepository Unspsc { get; }

    bool Save();
    Task<bool> SaveAsync();
}