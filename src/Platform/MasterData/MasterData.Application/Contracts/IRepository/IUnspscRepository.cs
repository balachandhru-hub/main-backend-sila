using MasterData.Domain.Entities;
using MasterData.Domain.Dto;
namespace MasterData.Application.Contracts.IRepository;

public interface IUnspscRepository
{
    Task CreateAsync(UnspscCategory entity);

    Task CreateRangeAsync(IEnumerable<UnspscCategory> entities);
     Task<List<UnspscDto>> GetAsync(
    int pageIndex,
    int pageSize);
    Task<List<UnspscDto>> GetByVersionAsync(
    string version,
    int pageIndex,
    int pageSize);
}