using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WeeklyBucketApprovalFlowRepository : RepositoryBase<WeeklyBucketApprovalFlow>, IWeeklyBucketApprovalFlowRepository
    {
        public WeeklyBucketApprovalFlowRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
