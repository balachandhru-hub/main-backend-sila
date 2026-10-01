using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class DocumentPageRepository : RepositoryBase<DocumentPage>, IDocumentPageRepository
    {
        public DocumentPageRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
