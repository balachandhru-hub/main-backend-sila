using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQAwardRepository
        : RepositoryBase<RFQAward>, IRFQAwardRepository
    {
        public RFQAwardRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
