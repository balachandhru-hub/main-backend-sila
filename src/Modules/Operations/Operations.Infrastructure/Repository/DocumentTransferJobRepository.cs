using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class DocumentTransferJobRepository : RepositoryBase<DocumentTransferJob>, IDocumentTransferJobRepository
    {
        public DocumentTransferJobRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<DocumentTransferJob>> ListDueAsync(DateTime now, int take, CancellationToken cancellationToken)
        {
            return RepositoryContext.DocumentTransferJob
                .AsNoTracking()
                .Where(x => (x.Status == DocumentTransferStatus.PENDING || x.Status == DocumentTransferStatus.RETRY_PENDING)
                    && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                .OrderBy(x => x.NextAttemptAt)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public Task<DocumentTransferJob?> GetTrackedAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return RepositoryContext.DocumentTransferJob
                .Include(x => x.Document)
                .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        }
    }
}
