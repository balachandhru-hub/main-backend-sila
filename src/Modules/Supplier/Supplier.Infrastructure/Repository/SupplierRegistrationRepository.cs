
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
public class SupplierRegistrationRepository
    : RepositoryBase<SupplierRegistration>,
      ISupplierRegistrationRepository
{
    public SupplierRegistrationRepository(
        RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}
}