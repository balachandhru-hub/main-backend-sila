using MasterData.Application.Contracts.IRepository;

namespace MasterData.Application.Contracts.IRepository;

public interface IRepositoryWrapper
{
    IUnspscRepository Unspsc { get; }

    Task SaveAsync();
}