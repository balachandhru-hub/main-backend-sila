using Buyer.Domain.Dto;

namespace Buyer.Application.Contracts
{
    public interface ISupplierApiClient
    {
        Task CreateSupplierRFQ(
            CreateSupplierRFQRequestDto rfq,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}