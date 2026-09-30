using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for MasterApprovalFlow.
    /// </summary>
    public class  MasterApprovalFlowRepositoty
        : RepositoryBase<MasterApprovalFlow>,
          IMasterApprovalFlowRepository
    {
        public  MasterApprovalFlowRepositoty(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}