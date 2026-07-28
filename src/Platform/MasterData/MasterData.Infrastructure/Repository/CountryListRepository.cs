using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class CountryListRepository : RepositoryBase<CountryList>, ICountryListRepository
{
    public CountryListRepository(RepositoryContext context) : base(context)
    {
    }
}
