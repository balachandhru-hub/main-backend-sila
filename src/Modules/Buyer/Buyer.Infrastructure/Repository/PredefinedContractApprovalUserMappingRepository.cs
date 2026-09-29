using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class PredefinedContractApprovalUserMappingRepository
        : RepositoryBase<PredefinedContractApprovalUserMapping>,
          IPredefinedContractApprovalUserMappingRepository
    {
        public PredefinedContractApprovalUserMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
