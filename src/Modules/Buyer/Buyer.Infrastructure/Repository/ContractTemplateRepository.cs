using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class ContractTemplateRepository : RepositoryBase<ContractTemplate>, IContractTemplateRepository
    {
        public ContractTemplateRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
