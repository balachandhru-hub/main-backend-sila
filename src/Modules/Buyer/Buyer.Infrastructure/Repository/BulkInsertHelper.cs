using EFCore.BulkExtensions;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository;

public class BulkInsertHelper
    : IBulkInsertHelper
{
    private readonly RepositoryContext _context;
    private readonly IUserIdentityService _userIdentityService;

    public BulkInsertHelper(
    RepositoryContext context,
    IUserIdentityService userIdentityService)
    {
        _context = context;
        _userIdentityService = userIdentityService;
    }

    public async Task BulkInsertOrUpdateAsync(List<ItemBuyerMaster> entities)
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
        nameof(ItemBuyerMaster.MaterialCode)
    }
        };
        Guid userId = _userIdentityService.GetCurrentUser();

        ApplyAuditFields(entities, userId);
        await _context.BulkInsertOrUpdateAsync(
            entities,
            bulkConfig);
    }
    private void ApplyAuditFields(
    List<ItemBuyerMaster> entities,
    Guid userId)
    {
        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            entity.DateUpdated = now;
            entity.UpdatedBy = userId;

            if (entity.DateCreated == default)
            {
                entity.DateCreated = now;
                entity.CreatedBy = userId;
                entity.IsActive = true;
            }
        }
    }
}
