using Supplier.Domain.Dto;

namespace Supplier.Application.Contracts
{
    public interface IBuyerApiClient
    {
        Task<List<Guid>> GetVerifiedSuppliers(
            GetVerifiedSupplierRequestDto request,
            
            CancellationToken cancellationToken = default);
    }
}