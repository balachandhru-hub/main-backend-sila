using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for PredefinedMaterial.
    /// </summary>
    public class  ApprovalFlowPredefinedMaterialMappingRepository
        : RepositoryBase<ApprovalFlowPredefinedMaterialMapping>,
          IApprovalFlowPredefinedMaterialMappingRepository
    {
        public  ApprovalFlowPredefinedMaterialMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}