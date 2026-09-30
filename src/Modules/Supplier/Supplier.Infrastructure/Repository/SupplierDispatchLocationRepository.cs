

using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierDispatchLocationRepository
    : RepositoryBase<SupplierDispatchLocation>,
      ISupplierDispatchLocationRepository
{
    public SupplierDispatchLocationRepository(
        RepositoryContext context)
        : base(context)
    {
    }
}
}