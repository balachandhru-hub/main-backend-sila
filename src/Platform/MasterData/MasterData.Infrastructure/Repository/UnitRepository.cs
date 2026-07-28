using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class UnitRepository : RepositoryBase<Unit>, IUnitRepository
{
    public UnitRepository(RepositoryContext context) : base(context) { }
}
