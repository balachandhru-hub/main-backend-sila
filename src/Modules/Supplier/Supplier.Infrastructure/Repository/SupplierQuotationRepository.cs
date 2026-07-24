using Microsoft.EntityFrameworkCore;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierQuotationRepository
        : RepositoryBase<SupplierQuotation>, ISupplierQuotationRepository
    {
        public SupplierQuotationRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<SupplierQuotation?> GetByIdAsync(Guid id)
        {
            return await RepositoryContext.SupplierQuotation
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}