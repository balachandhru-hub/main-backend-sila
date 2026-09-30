using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Repository for PredefinedMaterial.
    /// </summary>
    public class PredefinedMaterialApprovalFlowUserMappingRepository
        : RepositoryBase<PredefinedMaterialApprovalFlowUserMapping>,
          IPredefinedMaterialApprovalFlowUserMappingRepository
    {
        public PredefinedMaterialApprovalFlowUserMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}