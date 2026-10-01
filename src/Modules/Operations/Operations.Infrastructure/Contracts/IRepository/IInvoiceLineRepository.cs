using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IInvoiceLineRepository : IRepositoryBase<InvoiceLine>
    {
        Task<List<InvoiceLine>> GetTrackedByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);
    }
}
