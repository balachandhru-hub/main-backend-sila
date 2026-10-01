using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IApiFieldMappingRepository : IRepositoryBase<ApiFieldMapping>
    {
        Task<List<ApiFieldMapping>> GetTrackedByConfigurationAsync(Guid configurationId, CancellationToken cancellationToken);
    }
}
