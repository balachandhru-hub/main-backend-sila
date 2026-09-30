using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class ApiConfigRepository :
    RepositoryBase<ApiConfig>,
    IApiConfigRepository
{
    public ApiConfigRepository(
        RepositoryContext repositoryContext)
        : base(repositoryContext)
    {

    }
}