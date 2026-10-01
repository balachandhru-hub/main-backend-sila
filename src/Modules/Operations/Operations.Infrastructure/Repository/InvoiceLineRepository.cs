using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class InvoiceLineRepository : RepositoryBase<InvoiceLine>, IInvoiceLineRepository
    {
        public InvoiceLineRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<InvoiceLine>> GetTrackedByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken)
        {
            return RepositoryContext.InvoiceLine
                .Where(x => x.InvoiceId == invoiceId)
                .OrderBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);
        }
    }
}
