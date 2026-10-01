using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class DocumentRepository : RepositoryBase<Document>, IDocumentRepository
    {
        public DocumentRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<Document?> GetTrackedAsync(Guid documentId, Guid organizationId, CancellationToken cancellationToken)
        {
            return RepositoryContext.Document
                .FirstOrDefaultAsync(x => x.Id == documentId && x.OrganizationId == organizationId, cancellationToken);
        }
    }
}
