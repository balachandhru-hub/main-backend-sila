using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IInvoiceRepository : IRepositoryBase<Invoice>
    {
        Task<Invoice?> GetTrackedAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken);
        Task<Invoice?> GetTrackedByDocumentAsync(Guid documentId, Guid organizationId, CancellationToken cancellationToken);
    }
}
