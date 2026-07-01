using Microsoft.EntityFrameworkCore;
using MasterData.Application.Contracts.IRepository;
using MasterData.Domain.Dto;
using MasterData.Domain.Entities;
using MasterData.Infrastructure.Persistence;

public class UnspscRepository : IUnspscRepository
{
    private readonly AppDbContext _context;

    public UnspscRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(UnspscCategory entity)
    {
        await _context.UnspscCategories.AddAsync(entity);
    }

    public async Task CreateRangeAsync(IEnumerable<UnspscCategory> entities)
    {
        await _context.UnspscCategories.AddRangeAsync(entities);
    }
   public async Task<List<UnspscDto>> GetAsync(
    int pageIndex,
    int pageSize)
{
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