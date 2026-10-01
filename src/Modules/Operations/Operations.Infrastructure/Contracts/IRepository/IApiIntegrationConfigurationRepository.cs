using Operations.Domain.Enums;
using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IApiIntegrationConfigurationRepository : IRepositoryBase<ApiIntegrationConfiguration>
    {
        Task<bool> TryClaimAsync(Guid configurationId, Guid organizationId, CancellationToken cancellationToken);
        Task<List<ApiIntegrationConfiguration>> ListDueAsync(DateTime now, int take, CancellationToken cancellationToken);
    }
}
