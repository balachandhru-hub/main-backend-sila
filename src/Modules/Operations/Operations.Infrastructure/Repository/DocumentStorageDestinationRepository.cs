using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class DocumentStorageDestinationRepository : RepositoryBase<DocumentStorageDestination>, IDocumentStorageDestinationRepository
    {
        public DocumentStorageDestinationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
