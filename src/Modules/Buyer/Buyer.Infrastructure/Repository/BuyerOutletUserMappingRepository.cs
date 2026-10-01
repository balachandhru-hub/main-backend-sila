using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class BuyerOutletUserMappingRepository : RepositoryBase<BuyerOutletUserMapping>, IBuyerOutletUserMappingRepository
    {
        public BuyerOutletUserMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
