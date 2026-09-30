using Microsoft.EntityFrameworkCore;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class SupplierErpRepository : RepositoryBase<SupplierErpIntegrationConfiguration>, ISupplierErpRepository
    {
        public SupplierErpRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<SupplierErpIntegrationConfiguration?> GetByOrganizationAsync(Guid supplierOrganizationId, CancellationToken cancellationToken)
        {
            return RepositoryContext.SupplierErpIntegrationConfiguration
                .Where(x => x.SupplierOrganizationId == supplierOrganizationId)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<SupplierPurchaseDocument?> GetDocumentByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
        {
            return RepositoryContext.SupplierPurchaseDocument
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey && x.IsActive, cancellationToken);
        }

        public void AddDocument(SupplierPurchaseDocument document)
        {
            RepositoryContext.SupplierPurchaseDocument.Add(document);
        }
    }
}
