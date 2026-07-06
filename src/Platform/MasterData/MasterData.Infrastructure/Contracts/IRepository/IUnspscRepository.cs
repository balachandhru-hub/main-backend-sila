using MasterData.Domain.Dto;
using MasterData.Domain.Entities;

namespace MasterData.Infrastructure.Contracts.IRepository;

public interface IUnspscRepository
{
    Task CreateAsync(UnspscCategory entity);

    Task CreateRangeAsync(IEnumerable<UnspscCategory> entities);

    Task<List<SegmentDto>> GetAsync(
        int pageIndex,
        int pageSize);

     Task<List<ClassDto>> GetByVersionAsync(
        long segment,
        long family,
        int pageIndex,
        int pageSize);
}