using MasterData.Application.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly AppDbContext _context;

    public RepositoryWrapper(
        AppDbContext context,
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