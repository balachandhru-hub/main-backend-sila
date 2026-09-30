using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class MetadataRepository : RepositoryBase<Metadata>, IMetadataRepository
{
    public MetadataRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}