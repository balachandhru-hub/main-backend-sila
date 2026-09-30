using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQQuestionOptionRepository
        : RepositoryBase<RFQQuestionOption>, IRFQQuestionOptionRepository
    {
        public RFQQuestionOptionRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}