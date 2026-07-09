using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierBusinessProfileRepository
        : RepositoryBase<SupplierBusinessProfile>,
          ISupplierBusinessProfileRepository
    {
        public SupplierBusinessProfileRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}   