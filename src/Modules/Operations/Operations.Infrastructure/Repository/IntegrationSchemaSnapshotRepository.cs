using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class IntegrationSchemaSnapshotRepository : RepositoryBase<IntegrationSchemaSnapshot>, IIntegrationSchemaSnapshotRepository
    {
        public IntegrationSchemaSnapshotRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
