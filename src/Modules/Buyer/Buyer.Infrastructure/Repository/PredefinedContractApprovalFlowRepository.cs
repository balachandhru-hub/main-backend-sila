using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for PredefinedContractApprovalFlow.
    /// </summary>
    public class PredefinedContractApprovalFlowRepository
        : RepositoryBase<PredefinedContractApprovalFlow>,
          IPredefinedContractApprovalFlowRepository
    {
        public PredefinedContractApprovalFlowRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
