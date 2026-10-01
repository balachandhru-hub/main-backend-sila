using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class DocumentExtractionRepository : RepositoryBase<DocumentExtraction>, IDocumentExtractionRepository
    {
        public DocumentExtractionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
