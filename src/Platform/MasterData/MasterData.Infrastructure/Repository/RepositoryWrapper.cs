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
    private readonly IUnspscRepository _unspscRepository;//private IUnspscRepository _unspscRepository;should NOT be readonly because lazy initialization.

    private IApiConfigRepository _apiConfig;
    private IEmailContentRepository _emailContent;
    private IEmailSentDetailRepository _emailSentDetail;
    private IEmailFailedDetailRepository _emailFailedDetail;
    private IEmailCCListRepository _emailCCList;

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
    public IApiConfigRepository ApiConfig
    {
        get
        {
            if (_apiConfig == null)
                _apiConfig = new ApiConfigRepository(_context);

            return _apiConfig;
        }
    }


    public IEmailContentRepository EmailContent
    {
        get
        {
            if (_emailContent == null)
                _emailContent = new EmailContentRepository(_context);

            return _emailContent;
        }
    }


    public IEmailSentDetailRepository EmailSentDetail
    {
        get
        {
            if (_emailSentDetail == null)
                _emailSentDetail = new EmailSentDetailRepository(_context);

            return _emailSentDetail;
        }
    }


    public IEmailFailedDetailRepository EmailFailedDetail
    {
        get
        {
            if (_emailFailedDetail == null)
                _emailFailedDetail = new EmailFailedDetailRepository(_context);

            return _emailFailedDetail;
        }
    }


    public IEmailCCListRepository EmailCCList
    {
        get
        {
            if (_emailCCList == null)
                _emailCCList = new EmailCCListRepository(_context);

            return _emailCCList;
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