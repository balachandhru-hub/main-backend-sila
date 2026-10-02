using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IWeeklyBucketRepository : IRepositoryBase<WeeklyBucket>
    {
        Task<WeeklyBucket?> GetTrackedAsync(Guid weeklyBucketId, Guid buyerId, CancellationToken cancellationToken);
        Task<WeeklyBucket?> GetTrackedByIdAsync(Guid weeklyBucketId, CancellationToken cancellationToken);
        Task<List<WeeklyBucket>> ListFromWeekAsync(Guid buyerId, Guid propertyId, int year, int weekNumber, CancellationToken cancellationToken);
        Task<List<WeeklyBucketItem>> GetItemsAsync(Guid weeklyBucketId, CancellationToken cancellationToken);
        Task<WeeklyBucketItem?> GetItemAsync(Guid weeklyBucketId, Guid itemId, CancellationToken cancellationToken);
        Task<List<WeeklyBucketRecommendation>> GetRecommendationsAsync(Guid weeklyBucketId, CancellationToken cancellationToken);
        Task<BuyerOutlet?> GetOutletAsync(Guid outletId, Guid buyerId, CancellationToken cancellationToken);
        Task<List<BuyerOutlet>> ListOutletsAsync(Guid buyerId, CancellationToken cancellationToken);
        Task<BuyerProperty?> GetPropertyAsync(Guid propertyId, Guid buyerId, CancellationToken cancellationToken);
        Task<List<PurchaseDocumentIntegration>> GetIntegrationsAsync(Guid weeklyBucketId, CancellationToken cancellationToken);
        Task<WeeklyBucketApprovalFlow?> GetApprovalFlowAsync(Guid weeklyBucketId, CancellationToken cancellationToken);
        Task<List<WeeklyBucketApprovalUserMapping>> GetApprovalUsersAsync(Guid approvalFlowId, CancellationToken cancellationToken);
        Task<List<WeeklyBucketAudit>> GetAuditAsync(Guid weeklyBucketId, CancellationToken cancellationToken);
    }
}
