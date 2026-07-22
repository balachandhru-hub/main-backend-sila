using Identity.Domain.Dto;

namespace Identity.Application.Contracts
{
    public interface IBuyerApiClient
    {
        Task UpdateStatus(
            UpdateOrganizationStatusDto buyer,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}