using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WishlistItemRepository : RepositoryBase<WishlistItem>, IWishlistItemRepository
    {
        public WishlistItemRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
