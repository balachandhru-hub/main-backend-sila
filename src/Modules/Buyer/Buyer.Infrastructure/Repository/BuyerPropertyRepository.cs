using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class BuyerPropertyRepository : RepositoryBase<BuyerProperty>, IBuyerPropertyRepository
    {
        public BuyerPropertyRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
