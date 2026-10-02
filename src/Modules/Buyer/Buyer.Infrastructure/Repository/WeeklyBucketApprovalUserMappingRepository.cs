using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WeeklyBucketApprovalUserMappingRepository : RepositoryBase<WeeklyBucketApprovalUserMapping>, IWeeklyBucketApprovalUserMappingRepository
    {
        public WeeklyBucketApprovalUserMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
