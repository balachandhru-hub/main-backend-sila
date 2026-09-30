using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQBlockchainRecordRepository
        : RepositoryBase<RFQBlockchainRecord>,
          IRFQBlockchainRecordRepository
    {
        public RFQBlockchainRecordRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}