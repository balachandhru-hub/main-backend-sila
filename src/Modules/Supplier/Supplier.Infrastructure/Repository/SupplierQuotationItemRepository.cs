using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierQuotationItemRepository : RepositoryBase<SupplierQuotationItem>, ISupplierQuotationItemRepository
    {
        public SupplierQuotationItemRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}