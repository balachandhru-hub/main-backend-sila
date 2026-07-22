using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQQuestionRepository
        : RepositoryBase<RFQQuestion>, IRFQQuestionRepository
    {
        public RFQQuestionRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}