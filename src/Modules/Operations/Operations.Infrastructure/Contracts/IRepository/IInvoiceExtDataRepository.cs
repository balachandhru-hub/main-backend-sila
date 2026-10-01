using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IInvoiceExtDataRepository : IRepositoryBase<InvoiceExtData>
    {
        Task<List<InvoiceExtData>> GetTrackedByDocumentAsync(Guid documentId, CancellationToken cancellationToken);
    }
}
