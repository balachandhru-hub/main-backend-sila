using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class PersonalWishlistItemRepository : RepositoryBase<PersonalWishlistItem>, IPersonalWishlistItemRepository
    {
        public PersonalWishlistItemRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
