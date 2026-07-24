using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierRFQItemRepository
        : RepositoryBase<SupplierRFQItem>,
          ISupplierRFQItemRepository
    {
        public SupplierRFQItemRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}