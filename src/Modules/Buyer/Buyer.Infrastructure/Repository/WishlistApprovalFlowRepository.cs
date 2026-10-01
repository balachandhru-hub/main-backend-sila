using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WishlistApprovalFlowRepository : RepositoryBase<WishlistApprovalFlow>, IWishlistApprovalFlowRepository
    {
        public WishlistApprovalFlowRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
