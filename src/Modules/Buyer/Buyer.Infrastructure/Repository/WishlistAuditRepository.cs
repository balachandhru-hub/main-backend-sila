using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WishlistAuditRepository : RepositoryBase<WishlistAudit>, IWishlistAuditRepository
    {
        public WishlistAuditRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
