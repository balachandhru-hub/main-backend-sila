using Supplier.Domain.Dto;

namespace Supplier.Application.Contracts
{
    public interface IIdentityApiClient
    {
        Task UpdateOrganization(
            UpdateOrganizationRequestDto organization,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}