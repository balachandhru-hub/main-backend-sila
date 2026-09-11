using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for PredefinedMaterial.
    /// </summary>
    public class  ApprovalFlowUserMappingRepository
        : RepositoryBase<ApprovalFlowUserMapping>,
         IApprovalFlowUserMappingRepository
    {
        public  ApprovalFlowUserMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}