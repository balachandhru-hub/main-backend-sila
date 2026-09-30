using Supplier.Domain.Entities;

namespace Supplier.Infrastructure.Contracts.IRepository
{
    public interface ISupplierEmailVerificationRepository
        : IRepositoryBase<SupplierEmailVerification>
    {
    }
}