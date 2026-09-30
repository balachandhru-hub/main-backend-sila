

using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierEmailVerificationRepository
    : RepositoryBase<SupplierEmailVerification>,
      ISupplierEmailVerificationRepository
{
    public SupplierEmailVerificationRepository(
        RepositoryContext context)
        : base(context)
    {
    }
}
}