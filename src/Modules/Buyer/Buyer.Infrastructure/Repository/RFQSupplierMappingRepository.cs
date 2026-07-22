using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class RFQSupplierMappingRepository
        : RepositoryBase<RFQSupplierMapping>,
          IRFQSupplierMappingRepository
    {
         public RFQSupplierMappingRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}