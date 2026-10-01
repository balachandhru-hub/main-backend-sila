using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WishlistApprovalUserMappingRepository : RepositoryBase<WishlistApprovalUserMapping>, IWishlistApprovalUserMappingRepository
    {
        public WishlistApprovalUserMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
