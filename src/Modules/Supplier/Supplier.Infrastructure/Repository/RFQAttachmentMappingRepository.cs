using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class RFQAttachmentMappingRepository
        : RepositoryBase<RFQAttachmentMapping>,
          IRFQAttachmentMappingRepository
    {
        public RFQAttachmentMappingRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
