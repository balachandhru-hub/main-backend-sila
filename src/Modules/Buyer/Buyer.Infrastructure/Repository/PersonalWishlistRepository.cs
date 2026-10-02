using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repository
{
    public class PersonalWishlistRepository : RepositoryBase<PersonalWishlist>, IPersonalWishlistRepository
    {
        public PersonalWishlistRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        // A personal wishlist is visible only to its owner, so the owner is part of every lookup.
        public Task<PersonalWishlist?> GetTrackedAsync(Guid wishlistId, Guid buyerId, Guid ownerUserId, CancellationToken cancellationToken)
        {
            return RepositoryContext.PersonalWishlist
                .FirstOrDefaultAsync(
                    x => x.Id == wishlistId && x.BuyerId == buyerId && x.OwnerUserId == ownerUserId && x.IsActive,
                    cancellationToken);
        }

        public Task<List<PersonalWishlistItem>> GetItemsAsync(Guid wishlistId, CancellationToken cancellationToken)
        {
            return RepositoryContext.PersonalWishlistItem
                .Where(x => x.PersonalWishlistId == wishlistId && x.IsActive)
                .OrderBy(x => x.DateCreated)
                .ToListAsync(cancellationToken);
        }

        public Task<PersonalWishlistItem?> GetItemAsync(Guid wishlistId, Guid itemId, CancellationToken cancellationToken)
        {
            return RepositoryContext.PersonalWishlistItem
                .FirstOrDefaultAsync(x => x.Id == itemId && x.PersonalWishlistId == wishlistId && x.IsActive, cancellationToken);
        }
    }
}
