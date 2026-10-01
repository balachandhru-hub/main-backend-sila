using Supplier.Domain.Entities;

namespace Supplier.Infrastructure.Contracts.IRepository
{
    public interface ISupplierErpRepository : IRepositoryBase<SupplierErpIntegrationConfiguration>
    {
        Task<SupplierErpIntegrationConfiguration?> GetByOrganizationAsync(Guid supplierOrganizationId, CancellationToken cancellationToken);
        Task<SupplierPurchaseDocument?> GetDocumentByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
        void AddDocument(SupplierPurchaseDocument document);
    }
}
