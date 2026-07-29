using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierCategoryRepository
        : RepositoryBase<SupplierCategory>,
          ISupplierCategoryRepository
    {
        public SupplierCategoryRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
