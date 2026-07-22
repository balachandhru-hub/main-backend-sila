using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQAttachmentMappingRepository
        : RepositoryBase<RFQAttachmentMapping>,
          IRFQAttachmentMappingRepository
    {
        public RFQAttachmentMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}