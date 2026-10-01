using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IErpIntegrationRepository : IRepositoryBase<ErpIntegrationConfiguration>
    {
        Task<List<ErpIntegrationConfiguration>> ListAsync(Guid buyerId, CancellationToken cancellationToken);
        Task<ErpIntegrationConfiguration?> GetTrackedAsync(Guid buyerId, Guid configurationId, CancellationToken cancellationToken);
        Task<ErpIntegrationConfiguration?> FindForOperationAsync(Guid buyerId, string operation, CancellationToken cancellationToken);
    }
}
