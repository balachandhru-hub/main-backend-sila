using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IWishlistRepository : IRepositoryBase<Wishlist>
    {
        Task<Wishlist?> GetTrackedAsync(Guid wishlistId, Guid buyerId, CancellationToken cancellationToken);
        Task<Wishlist?> GetTrackedByIdAsync(Guid wishlistId, CancellationToken cancellationToken);
        Task<List<WishlistItem>> GetItemsAsync(Guid wishlistId, CancellationToken cancellationToken);
        Task<BuyerOutlet?> GetOutletAsync(Guid outletId, Guid buyerId, CancellationToken cancellationToken);
        Task<List<BuyerOutlet>> ListOutletsAsync(Guid buyerId, CancellationToken cancellationToken);
        Task<ErpIntegrationConfiguration?> GetErpConfigurationAsync(Guid buyerId, CancellationToken cancellationToken);
        Task<ErpIntegrationConfiguration?> GetErpConfigurationByIdAsync(Guid configurationId, CancellationToken cancellationToken);
        Task<List<PurchaseDocumentIntegration>> GetIntegrationsAsync(Guid wishlistId, CancellationToken cancellationToken);
        Task<PurchaseDocumentIntegration?> GetIntegrationAsync(Guid wishlistId, string integrationType, Guid supplierOrganizationId, CancellationToken cancellationToken);
        Task<WishlistApprovalFlow?> GetApprovalFlowAsync(Guid wishlistId, CancellationToken cancellationToken);
        Task<List<WishlistApprovalUserMapping>> GetApprovalUsersAsync(Guid approvalFlowId, CancellationToken cancellationToken);
        Task<List<WishlistAudit>> GetAuditAsync(Guid wishlistId, CancellationToken cancellationToken);
        Task<List<Guid>> GetDueWishlistIdsAsync(DateTime utcNow, CancellationToken cancellationToken);
        void Add<T>(T entity) where T : class;
        void RemoveRange<T>(IEnumerable<T> entities) where T : class;
    }
}
