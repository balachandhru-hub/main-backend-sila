using EFCore.BulkExtensions;
using MasterData.Application.Contracts.IRepository;
using MasterData.Domain.Entities;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class BulkInsertHelper : IBulkInsertHelper
{
    private readonly RepositoryContext _context;

    public BulkInsertHelper(RepositoryContext context)
    {
        _context = context;
    }

    public async Task BulkInsertOrUpdateAsync(List<UnspscCategory> entities)
    {
        if (!entities.Any())
            return;

        var bulkConfig = new BulkConfig
        {
            BatchSize = 2000,

            PreserveInsertOrder = true,

            SetOutputIdentity = false,

            UpdateByProperties = new List<string>
            {
                nameof(UnspscCategory.Segment),
                nameof(UnspscCategory.Family),
                nameof(UnspscCategory.Class),
                nameof(UnspscCategory.Commodity)
            }
        };

        await _context.BulkInsertOrUpdateAsync(
            entities,
            bulkConfig);
    }
}