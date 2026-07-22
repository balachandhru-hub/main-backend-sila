using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQRepository
        : RepositoryBase<RFQ>, IRFQRepository
    {
        public RFQRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}