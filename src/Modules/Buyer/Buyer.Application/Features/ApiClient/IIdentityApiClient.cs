using Buyer.Domain.Dto;

namespace Buyer.Application.Contracts
{
    public interface IIdentityApiClient
    {
        Task UpdateOrganization(
            UpdateBuyerBusinessProfileDto organization,
            string accessToken,
            CancellationToken cancellationToken = default);
        Task<List<ModelDto>> GetOrganizationModels(
Guid? organizationId = null,
CancellationToken cancellationToken = default);
    }
}