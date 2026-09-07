using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class ExternalSupplierRepository
        : RepositoryBase<ExternalSupplier>,
          IExternalSupplierRepository
    {
         public ExternalSupplierRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
