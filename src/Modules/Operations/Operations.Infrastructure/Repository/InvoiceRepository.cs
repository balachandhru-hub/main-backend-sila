using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class InvoiceRepository : RepositoryBase<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<Invoice?> GetTrackedAsync(Guid invoiceId, Guid organizationId, CancellationToken cancellationToken)
        {
            return RepositoryContext.Invoice
                .Include(x => x.Document)
                .FirstOrDefaultAsync(x => x.Id == invoiceId && x.OrganizationId == organizationId, cancellationToken);
        }

        public Task<Invoice?> GetTrackedByDocumentAsync(Guid documentId, Guid organizationId, CancellationToken cancellationToken)
        {
            return RepositoryContext.Invoice
                .Include(x => x.Document)
                .FirstOrDefaultAsync(x => x.DocumentId == documentId && x.OrganizationId == organizationId, cancellationToken);
        }
    }
}
