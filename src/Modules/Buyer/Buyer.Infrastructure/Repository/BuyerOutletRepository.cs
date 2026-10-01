using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class BuyerOutletRepository : RepositoryBase<BuyerOutlet>, IBuyerOutletRepository
    {
        public BuyerOutletRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
