using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repository
{
    public class WeeklyBucketRepository : RepositoryBase<WeeklyBucket>, IWeeklyBucketRepository
    {
        public WeeklyBucketRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<WeeklyBucket?> GetTrackedAsync(Guid weeklyBucketId, Guid buyerId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucket
                .FirstOrDefaultAsync(x => x.Id == weeklyBucketId && x.BuyerId == buyerId && x.IsActive, cancellationToken);
        }

        public Task<WeeklyBucket?> GetTrackedByIdAsync(Guid weeklyBucketId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucket
                .FirstOrDefaultAsync(x => x.Id == weeklyBucketId && x.IsActive, cancellationToken);
        }

        // Buckets of the property from the given week onwards, oldest first.
        public Task<List<WeeklyBucket>> ListFromWeekAsync(Guid buyerId, Guid propertyId, int year, int weekNumber, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucket
                .Where(x => x.BuyerId == buyerId
                            && x.PropertyId == propertyId
                            && x.IsActive
                            && (x.Year > year || (x.Year == year && x.WeekNumber >= weekNumber)))
                .OrderBy(x => x.Year)
                .ThenBy(x => x.WeekNumber)
                .ToListAsync(cancellationToken);
        }

        public Task<List<WeeklyBucketItem>> GetItemsAsync(Guid weeklyBucketId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucketItem
                .Where(x => x.WeeklyBucketId == weeklyBucketId && x.IsActive)
                .OrderBy(x => x.DateCreated)
                .ToListAsync(cancellationToken);
        }

        public Task<WeeklyBucketItem?> GetItemAsync(Guid weeklyBucketId, Guid itemId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucketItem
                .FirstOrDefaultAsync(x => x.Id == itemId && x.WeeklyBucketId == weeklyBucketId && x.IsActive, cancellationToken);
        }

        public Task<List<WeeklyBucketRecommendation>> GetRecommendationsAsync(Guid weeklyBucketId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucketRecommendation
                .Where(x => x.WeeklyBucketId == weeklyBucketId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync(cancellationToken);
        }

        public Task<BuyerOutlet?> GetOutletAsync(Guid outletId, Guid buyerId, CancellationToken cancellationToken)
        {
            return RepositoryContext.BuyerOutlet
                .FirstOrDefaultAsync(x => x.Id == outletId && x.BuyerId == buyerId && x.IsActive, cancellationToken);
        }

        public Task<List<BuyerOutlet>> ListOutletsAsync(Guid buyerId, CancellationToken cancellationToken)
        {
            return RepositoryContext.BuyerOutlet
                .AsNoTracking()
                .Where(x => x.BuyerId == buyerId && x.IsActive)
                .OrderBy(x => x.OutletName)
                .ToListAsync(cancellationToken);
        }

        public Task<BuyerProperty?> GetPropertyAsync(Guid propertyId, Guid buyerId, CancellationToken cancellationToken)
        {
            return RepositoryContext.BuyerProperty
                .FirstOrDefaultAsync(x => x.Id == propertyId && x.BuyerId == buyerId && x.IsActive, cancellationToken);
        }

        public Task<List<PurchaseDocumentIntegration>> GetIntegrationsAsync(Guid weeklyBucketId, CancellationToken cancellationToken)
        {
            return RepositoryContext.PurchaseDocumentIntegration
                .Where(x => x.WeeklyBucketId == weeklyBucketId && x.IsActive)
                .OrderBy(x => x.DateCreated)
                .ToListAsync(cancellationToken);
        }

        public Task<WeeklyBucketApprovalFlow?> GetApprovalFlowAsync(Guid weeklyBucketId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucketApprovalFlow
                .FirstOrDefaultAsync(x => x.WeeklyBucketId == weeklyBucketId && x.IsActive, cancellationToken);
        }

        public Task<List<WeeklyBucketApprovalUserMapping>> GetApprovalUsersAsync(Guid approvalFlowId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucketApprovalUserMapping
                .Where(x => x.WeeklyBucketApprovalFlowId == approvalFlowId && x.IsActive)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);
        }

        public Task<List<WeeklyBucketAudit>> GetAuditAsync(Guid weeklyBucketId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WeeklyBucketAudit
                .AsNoTracking()
                .Where(x => x.WeeklyBucketId == weeklyBucketId && x.IsActive)
                .OrderBy(x => x.DateCreated)
                .ToListAsync(cancellationToken);
        }
    }
}
