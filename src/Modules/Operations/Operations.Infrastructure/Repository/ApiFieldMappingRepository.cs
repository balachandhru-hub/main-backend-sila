using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class ApiFieldMappingRepository : RepositoryBase<ApiFieldMapping>, IApiFieldMappingRepository
    {
        public ApiFieldMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<ApiFieldMapping>> GetTrackedByConfigurationAsync(Guid configurationId, CancellationToken cancellationToken)
        {
            return RepositoryContext.ApiFieldMapping
                .Where(x => x.ConfigurationId == configurationId)
                .ToListAsync(cancellationToken);
        }
    }
}
