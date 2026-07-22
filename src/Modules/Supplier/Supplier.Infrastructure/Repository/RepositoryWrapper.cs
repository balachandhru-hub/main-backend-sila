using Supplier.Infrastructure.DbContext;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IServices;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;


namespace Supplier.Infrastructure.Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly RepositoryContext _context;
       
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly string _dbConnectionString;
         private readonly IUserIdentityService _userIdentityService;
         private ISupplierBusinessProfileRepository _supplierBusinessProfile;

        private ISupplierRegistrationRepository _supplierRegistration;

        private ISupplierBankAccountRepository _supplierBankAccount;

        private ISupplierDispatchLocationRepository _supplierDispatchLocation;
        private IAssetRepository _asset;
        private ISupplierCatalogRepository _supplierCatalog;
  
      
        public RepositoryWrapper(RepositoryContext repositoryContext, IUserIdentityService userIdentityService, IConfiguration configuration, ILoggerManager logger)
        {
            _context = repositoryContext;
             _userIdentityService = userIdentityService;
            _logger = logger;
            _configuration = configuration;
            _dbConnectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
      public ISupplierBusinessProfileRepository SupplierBusinessProfile
        {
            get
            {
                if (_supplierBusinessProfile == null)
                {
                    _supplierBusinessProfile = new SupplierBusinessProfileRepository(_context);
                }
                return _supplierBusinessProfile;
            }
        }
        public ISupplierRegistrationRepository SupplierRegistration
        {
            get
            {
                if (_supplierRegistration == null)
                {
                    _supplierRegistration = new SupplierRegistrationRepository(_context);
                }
                return _supplierRegistration;
            }
        }
        public ISupplierBankAccountRepository SupplierBankAccount
        {
            get
            {
                if (_supplierBankAccount == null)
                {
                    _supplierBankAccount = new SupplierBankAccountRepository(_context);
                }
                return _supplierBankAccount;
            }
        }
        public ISupplierDispatchLocationRepository SupplierDispatchLocation
        {
            get
            {
                if (_supplierDispatchLocation == null)
                {
                    _supplierDispatchLocation = new SupplierDispatchLocationRepository(_context);
                }
                return _supplierDispatchLocation;
            }
        }
         public IAssetRepository Asset
        {
            get
            {
                if (_asset == null) _asset = new AssetRepository(_context, _configuration, _logger, _dbConnectionString);
                return _asset;
            }
        }

          public ISupplierCatalogRepository SupplierCatalog
        {
            get
            {
                if (_supplierCatalog == null) _supplierCatalog = new SupplierCatalogRepository(_context);
                return _supplierCatalog;
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