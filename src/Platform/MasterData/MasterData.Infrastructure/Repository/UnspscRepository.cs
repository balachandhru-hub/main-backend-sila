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

    public async Task<List<SegmentDto>> GetAsync(
    int pageIndex,
    int pageSize)
{
    _logger.LogInfo($"Retrieving UNSPSC categories. PageIndex: {pageIndex}, PageSize: {pageSize}");

    var data = await _context.UnspscCategories
        .OrderBy(x => x.Segment)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return data
        .GroupBy(x => new
        {
            x.Segment,
            x.SegmentTitle
        })
        .Select(segment => new SegmentDto
        {
            Segment = segment.Key.Segment,
            Title = segment.Key.SegmentTitle,

            Family = segment
                .Where(x => x.Family.HasValue)
                .GroupBy(x => new
                {
                    x.Family,
                    x.FamilyTitle
                })
                .Select(f => new FamilyDto
                {
                    Family = f.Key.Family,
                    Title = f.Key.FamilyTitle
                })
                .ToList()
        })
        .ToList();
}

   public async Task<List<ClassDto>> GetByVersionAsync(
    long segment,
    long family,
    int pageIndex,
    int pageSize)
{
    _logger.LogInfo($"Retrieving classes for Segment={segment}, Family={family}");

    var data = await _context.UnspscCategories
        .Where(x => x.Segment == segment &&
                    x.Family == family)
        .OrderBy(x => x.Class)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return data
        .GroupBy(x => new
        {
            x.Class,
            x.ClassTitle
        })
        .Select(c => new ClassDto
        {
            Class = c.Key.Class,
            Title = c.Key.ClassTitle,

            Commodity = c
                .Where(x => x.Commodity.HasValue)
                .GroupBy(x => new
                {
                    x.Commodity,
                    x.CommodityTitle
                })
                .Select(cm => new CommodityDto
                {
                    Commodity = cm.Key.Commodity,
                    Title = cm.Key.CommodityTitle
                })
                .ToList()
        })
        .ToList();
}
}