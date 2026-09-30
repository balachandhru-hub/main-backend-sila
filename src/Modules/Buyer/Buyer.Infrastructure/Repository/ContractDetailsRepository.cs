using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class ContractDetailsRepository : RepositoryBase<ContractDetails>, IContractDetailsRepository
    {
        public ContractDetailsRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
