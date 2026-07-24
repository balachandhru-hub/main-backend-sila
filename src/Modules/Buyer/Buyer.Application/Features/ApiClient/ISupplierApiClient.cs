using Buyer.Domain.Dto;

namespace Buyer.Application.Contracts
{
    public interface ISupplierApiClient
    {
        Task CreateSupplierRFQ(
            CreateSupplierRFQRequestDto rfq,
            
            CancellationToken cancellationToken = default);
    }
}