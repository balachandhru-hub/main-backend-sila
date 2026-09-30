using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQQuestionAttachmentMappingRepository
        : RepositoryBase<RFQQuestionAttachmentMapping>, IRFQQuestionAttachmentMappingRepository
    {
        public RFQQuestionAttachmentMappingRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}