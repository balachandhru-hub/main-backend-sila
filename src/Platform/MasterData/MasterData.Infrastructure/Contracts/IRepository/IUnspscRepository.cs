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

    Task<List<GetSegmentDto>> GetSegmentAsync(
    int pageIndex,
    int pageSize,
    string? searchTerm);

    Task<List<FamilyDto>> GetFamilyAsync(
    long segment,
    int pageIndex,
    int pageSize);
       Task<List<UnspscClassDto>> GetClassAsync(
            
            long family,
            int pageIndex,
            int pageSize);

        Task<List<UnspscCommodityDto>> GetCommodityAsync(
          
            long @class,
            int pageIndex,
            int pageSize);
}