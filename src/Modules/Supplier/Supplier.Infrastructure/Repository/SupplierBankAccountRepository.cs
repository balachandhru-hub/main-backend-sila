

using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{

public class SupplierBankAccountRepository
    : RepositoryBase<SupplierBankAccount>,
      ISupplierBankAccountRepository
{
    public SupplierBankAccountRepository(
        RepositoryContext context)
        : base(context)
    {
    }
}
}