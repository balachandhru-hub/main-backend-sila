using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for ContractApprovalFlow.
    /// </summary>
    public class ContractApprovalFlowRepository
        : RepositoryBase<ContractApprovalFlow>,
          IContractApprovalFlowRepository
    {
        public ContractApprovalFlowRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
