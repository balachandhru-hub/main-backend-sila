using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Infrastructure.Repository
{
    public class ErpIntegrationRepository : RepositoryBase<ErpIntegrationConfiguration>, IErpIntegrationRepository
    {
        public ErpIntegrationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<ErpIntegrationConfiguration>> ListAsync(Guid buyerId, CancellationToken cancellationToken)
        {
            return RepositoryContext.ErpIntegrationConfiguration
                .AsNoTracking()
                .Where(x => x.BuyerId == buyerId && x.IsActive)
                .OrderBy(x => x.Process)
                .ThenBy(x => x.ApiName)
                .ToListAsync(cancellationToken);
        }

        public Task<ErpIntegrationConfiguration?> GetTrackedAsync(Guid buyerId, Guid configurationId, CancellationToken cancellationToken)
        {
            return RepositoryContext.ErpIntegrationConfiguration
                .FirstOrDefaultAsync(x => x.Id == configurationId && x.BuyerId == buyerId, cancellationToken);
        }

        public Task<List<ErpIntegrationConfiguration>> ListForOperationAsync(
            Guid buyerId,
            string operation,
            Guid? supplierOrganizationId,
            CancellationToken cancellationToken)
        {
            string normalized = operation.Trim().ToUpperInvariant();
            return RepositoryContext.ErpIntegrationConfiguration
                .Where(x => x.BuyerId == buyerId
                    && x.IsActive
                    && x.Process.ToUpper() == normalized
                    && (x.SupplierOrganizationId == null || x.SupplierOrganizationId == supplierOrganizationId))
                .OrderBy(x => x.ErpType)
                .ThenBy(x => x.ApiName)
                .ToListAsync(cancellationToken);
        }
    }
}
