using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
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
