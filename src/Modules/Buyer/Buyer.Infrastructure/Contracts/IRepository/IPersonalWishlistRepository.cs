using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IPersonalWishlistRepository : IRepositoryBase<PersonalWishlist>
    {
        Task<PersonalWishlist?> GetTrackedAsync(Guid wishlistId, Guid buyerId, Guid ownerUserId, CancellationToken cancellationToken);
        Task<List<PersonalWishlistItem>> GetItemsAsync(Guid wishlistId, CancellationToken cancellationToken);
        Task<PersonalWishlistItem?> GetItemAsync(Guid wishlistId, Guid itemId, CancellationToken cancellationToken);
    }
}
