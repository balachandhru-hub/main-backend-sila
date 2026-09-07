using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class RFQExternalSupplierRepository
        : RepositoryBase<RFQExternalSupplier>,
          IRFQExternalSupplierRepository
    {
         public RFQExternalSupplierRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
