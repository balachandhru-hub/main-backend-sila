using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repository
{
    public class WishlistRepository : RepositoryBase<Wishlist>, IWishlistRepository
    {
        public WishlistRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<Wishlist?> GetTrackedAsync(Guid wishlistId, Guid buyerId, CancellationToken cancellationToken)
        {
            return RepositoryContext.Wishlist
                .FirstOrDefaultAsync(x => x.Id == wishlistId && x.BuyerId == buyerId && x.IsActive, cancellationToken);
        }

        public Task<Wishlist?> GetTrackedByIdAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            return RepositoryContext.Wishlist
                .FirstOrDefaultAsync(x => x.Id == wishlistId && x.IsActive, cancellationToken);
        }

        public Task<List<WishlistItem>> GetItemsAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WishlistItem
                .Where(x => x.WishlistId == wishlistId && x.IsActive)
                .OrderBy(x => x.DateCreated)
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

        public Task<List<PurchaseDocumentIntegration>> GetIntegrationsAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            return RepositoryContext.PurchaseDocumentIntegration
                .Where(x => x.WishlistId == wishlistId && x.IsActive)
                .OrderBy(x => x.DateCreated)
                .ToListAsync(cancellationToken);
        }

        public Task<PurchaseDocumentIntegration?> GetIntegrationAsync(
            Guid wishlistId,
            string integrationType,
            Guid supplierOrganizationId,
            CancellationToken cancellationToken)
        {
            return RepositoryContext.PurchaseDocumentIntegration
                .FirstOrDefaultAsync(
                    x => x.WishlistId == wishlistId
                         && x.IntegrationType == integrationType
                         && x.SupplierOrganizationId == supplierOrganizationId
                         && x.IsActive,
                    cancellationToken);
        }

        public Task<WishlistApprovalFlow?> GetApprovalFlowAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WishlistApprovalFlow
                .FirstOrDefaultAsync(x => x.WishlistId == wishlistId && x.IsActive, cancellationToken);
        }

        public Task<List<WishlistApprovalUserMapping>> GetApprovalUsersAsync(Guid approvalFlowId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WishlistApprovalUserMapping
                .Where(x => x.WishlistApprovalFlowId == approvalFlowId && x.IsActive)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);
        }

        public Task<List<WishlistAudit>> GetAuditAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            return RepositoryContext.WishlistAudit
                .AsNoTracking()
                .Where(x => x.WishlistId == wishlistId && x.IsActive)
                .OrderBy(x => x.DateCreated)
                .ToListAsync(cancellationToken);
        }
    }
}
