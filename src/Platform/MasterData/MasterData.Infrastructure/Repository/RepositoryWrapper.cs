using MasterData.Application.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly RepositoryContext _context;

    public RepositoryWrapper(
        RepositoryContext context,
        IUnspscRepository unspscRepository)
    {
        _context = context;
        Unspsc = unspscRepository;
    }

    public IUnspscRepository Unspsc { get; }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}