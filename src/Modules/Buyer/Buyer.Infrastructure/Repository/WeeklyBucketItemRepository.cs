using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WeeklyBucketItemRepository : RepositoryBase<WeeklyBucketItem>, IWeeklyBucketItemRepository
    {
        public WeeklyBucketItemRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
