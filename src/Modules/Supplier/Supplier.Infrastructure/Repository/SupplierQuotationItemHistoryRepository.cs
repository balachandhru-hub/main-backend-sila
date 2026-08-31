

using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierQuotationItemHistoryRepository
    : RepositoryBase<SupplierQuotationItemHistory>,
        ISupplierQuotationItemHistoryRepository
{
    public SupplierQuotationItemHistoryRepository(
        RepositoryContext context)
        : base(context)
    {
    }
}
}