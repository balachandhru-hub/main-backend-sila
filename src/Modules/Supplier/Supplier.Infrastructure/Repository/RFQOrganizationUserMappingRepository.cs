using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class RFQOrganizationUserMappingRepository
        : RepositoryBase<RFQOrganizationUserMapping>,
          IRFQOrganizationUserMappingRepository
    {
        public RFQOrganizationUserMappingRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
