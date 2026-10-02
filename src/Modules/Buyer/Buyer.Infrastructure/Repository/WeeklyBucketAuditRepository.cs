using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WeeklyBucketAuditRepository : RepositoryBase<WeeklyBucketAudit>, IWeeklyBucketAuditRepository
    {
        public WeeklyBucketAuditRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
