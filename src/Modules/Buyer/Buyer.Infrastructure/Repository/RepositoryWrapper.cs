using Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.Extensions.Configuration;
using Contracts;
using SharedKernel.LoggerServices;
using Buyer.Infrastructure.Contracts.IServices;
using Buyer.Domain.Entities;


namespace Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly RepositoryContext _context;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly string _dbConnectionString;
        private IBuyerBusinessProfileRepository _buyerBusinessProfileRepository;
        

        public RepositoryWrapper(RepositoryContext repositoryContext, IUserIdentityService userIdentityService, IConfiguration configuration, ILoggerManager logger)
        {
            _context = repositoryContext;
            _userIdentityService = userIdentityService;
            _logger = logger;
            _configuration = configuration;
            _dbConnectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
        public IBuyerBusinessProfileRepository BuyerBusinessProfile
        {
            get
            {
                if (_buyerBusinessProfileRepository == null)
                {
                    _buyerBusinessProfileRepository= new BuyerBusinessProfileRepository(_context);
                }
                return _buyerBusinessProfileRepository;
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
}