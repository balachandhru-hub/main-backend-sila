using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class RFQItemAttachmentMappingRepository
        : RepositoryBase<RFQItemAttachmentMapping>,
          IRFQItemAttachmentMappingRepository
    {
        public RFQItemAttachmentMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}