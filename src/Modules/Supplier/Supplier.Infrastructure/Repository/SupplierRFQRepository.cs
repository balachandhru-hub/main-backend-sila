using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;
namespace Supplier.Infrastructure.Repository
{
public class SupplierRFQRepository
    : RepositoryBase<SupplierRFQ>,
      ISupplierRFQRepository
{
    public SupplierRFQRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}
}


  