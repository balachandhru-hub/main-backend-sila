using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Contracts.IServices;
using MasterData.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace MasterData.Infrastructure.Repository;

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly RepositoryContext _context;
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILoggerManager _logger;
    private readonly IConfiguration _configuration;
    private readonly string _dbConnectionString;
    private readonly IUnspscRepository _unspscRepository;

    public RepositoryWrapper(
        RepositoryContext repositoryContext,
        IUserIdentityService userIdentityService,
        ILoggerManager logger,
        IConfiguration configuration
        )
       
    {
        _context = repositoryContext;
        _userIdentityService = userIdentityService;
        _logger = logger;
        _configuration = configuration;
         _dbConnectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

  public IUnspscRepository Unspsc
    {
        get
        {
            if (_unspscRepository == null)
            {
                return new UnspscRepository(_context, _logger);
            }
            return _unspscRepository;
        }
    }

    public bool Save()
    {
        _context.OnBeforeSaving(_userIdentityService.GetCurrentUser());
        _context.SaveChanges();
        return true;
    }

    public async Task<bool> SaveAsync()
    {
        _context.OnBeforeSaving(_userIdentityService.GetCurrentUser());
        await _context.SaveChangesAsync();
        return true;
    }
}