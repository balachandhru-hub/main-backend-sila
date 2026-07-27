using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class CurrencyRepository : RepositoryBase<Currency>, ICurrencyRepository
{
    public CurrencyRepository(RepositoryContext context) : base(context)
    {
    }
}
