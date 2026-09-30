using Identity.Domain.Dto;

namespace Identity.Application.Contracts
{
    public interface ISupplierApiClient
    {
        Task UpdateStatus(
            UpdateOrganizationStatusDto supplier,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}