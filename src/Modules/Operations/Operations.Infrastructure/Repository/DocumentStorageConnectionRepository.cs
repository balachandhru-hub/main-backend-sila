using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class DocumentStorageConnectionRepository : RepositoryBase<DocumentStorageConnection>, IDocumentStorageConnectionRepository
    {
        public DocumentStorageConnectionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
