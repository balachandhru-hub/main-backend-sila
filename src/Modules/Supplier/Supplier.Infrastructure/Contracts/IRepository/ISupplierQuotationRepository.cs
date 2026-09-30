using Supplier.Domain.Entities;

namespace Supplier.Infrastructure.Contracts.IRepository
{
    public interface ISupplierQuotationRepository
        : IRepositoryBase<SupplierQuotation>
    {
        Task<SupplierQuotation?> GetByIdAsync(Guid id);
    }
}