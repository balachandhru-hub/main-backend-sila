using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Infrastructure.Contracts.IRepository
{
    public interface ISupplierBusinessProfileRepository
        : IRepositoryBase<SupplierBusinessProfile>
    {
    }
}