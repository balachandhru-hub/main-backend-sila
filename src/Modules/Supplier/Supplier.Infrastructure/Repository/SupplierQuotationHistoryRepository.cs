

using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierQuotationHistoryRepository
    : RepositoryBase<SupplierQuotationHistory>,
        ISupplierQuotationHistoryRepository
{
    public SupplierQuotationHistoryRepository(
        RepositoryContext context)
        : base(context)
    {
    }
}
}