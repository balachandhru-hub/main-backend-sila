using Microsoft.EntityFrameworkCore;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Domain.Dto;
using MasterData.Domain.Entities;
using MasterData.Infrastructure.Persistence;
using SharedKernel.LoggerServices;
namespace MasterData.Infrastructure.Repository;

public class UnspscRepository : IUnspscRepository
{
    private readonly RepositoryContext _context;
     private readonly ILoggerManager _logger;

    public UnspscRepository(RepositoryContext context, ILoggerManager logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateAsync(UnspscCategory entity)
    {
        _logger.LogInfo($"Creating UNSPSC category with Key: {entity.Key}, Version: {entity.Version}");
        await _context.UnspscCategories.AddAsync(entity);
    }

    public async Task CreateRangeAsync(IEnumerable<UnspscCategory> entities)
    {
        _logger.LogInfo($"Creating range of UNSPSC categories. Count: {entities.Count()}");
        await _context.UnspscCategories.AddRangeAsync(entities);
    }

    public async Task<List<UnspscDto>> GetAsync(
        int pageIndex,
        int pageSize)
    {
        _logger.LogInfo($"Retrieving UNSPSC categories. PageIndex: {pageIndex}, PageSize: {pageSize}");
        return await _context.UnspscCategories
            .OrderBy(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UnspscDto
            {
                Version = x.Version,
                Key = x.Key,
                Segment = x.Segment,
                SegmentTitle = x.SegmentTitle,
                SegmentDefinition = x.SegmentDefinition,
                Family = x.Family,
                FamilyTitle = x.FamilyTitle,
                FamilyDefinition = x.FamilyDefinition,
                Class = x.Class,
                ClassTitle = x.ClassTitle,
                ClassDefinition = x.ClassDefinition,
                Commodity = x.Commodity,
                CommodityTitle = x.CommodityTitle,
                CommodityDefinition = x.CommodityDefinition,
                Synonym = x.Synonym,
                Acronym = x.Acronym
            })
            .ToListAsync();
    }

    public async Task<List<UnspscDto>> GetByVersionAsync(
        string version,
        int pageIndex,
        int pageSize)
    {
        _logger.LogInfo($"Retrieving UNSPSC categories by version: {version}. PageIndex: {pageIndex}, PageSize: {pageSize}");
        return await _context.UnspscCategories
            .Where(x => x.Version == version)
            .OrderBy(x => x.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UnspscDto
            {
                Version = x.Version,
                Key = x.Key,
                Segment = x.Segment,
                SegmentTitle = x.SegmentTitle,
                SegmentDefinition = x.SegmentDefinition,
                Family = x.Family,
                FamilyTitle = x.FamilyTitle,
                FamilyDefinition = x.FamilyDefinition,
                Class = x.Class,
                ClassTitle = x.ClassTitle,
                ClassDefinition = x.ClassDefinition,
                Commodity = x.Commodity,
                CommodityTitle = x.CommodityTitle,
                CommodityDefinition = x.CommodityDefinition,
                Synonym = x.Synonym,
                Acronym = x.Acronym
            })
            .ToListAsync();
    }
}