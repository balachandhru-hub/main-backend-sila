using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class InvoiceExtDataRepository : RepositoryBase<InvoiceExtData>, IInvoiceExtDataRepository
    {
        public InvoiceExtDataRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<InvoiceExtData>> GetTrackedByDocumentAsync(Guid documentId, CancellationToken cancellationToken)
        {
            return RepositoryContext.InvoiceExtData
                .Where(x => x.DocumentId == documentId)
                .OrderByDescending(x => x.DateUpdated)
                .ToListAsync(cancellationToken);
        }
    }
}
