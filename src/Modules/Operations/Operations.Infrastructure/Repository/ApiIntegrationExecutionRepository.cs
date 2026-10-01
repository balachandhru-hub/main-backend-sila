using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class ApiIntegrationExecutionRepository : RepositoryBase<ApiIntegrationExecution>, IApiIntegrationExecutionRepository
    {
        public ApiIntegrationExecutionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
