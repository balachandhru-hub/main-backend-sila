using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQAwardItemRepository
        : RepositoryBase<RFQAwardItem>, IRFQAwardItemRepository
    {
        public RFQAwardItemRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
