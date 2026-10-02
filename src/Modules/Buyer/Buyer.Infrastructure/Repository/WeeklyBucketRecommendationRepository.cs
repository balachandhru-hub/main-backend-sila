using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class WeeklyBucketRecommendationRepository : RepositoryBase<WeeklyBucketRecommendation>, IWeeklyBucketRecommendationRepository
    {
        public WeeklyBucketRecommendationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
