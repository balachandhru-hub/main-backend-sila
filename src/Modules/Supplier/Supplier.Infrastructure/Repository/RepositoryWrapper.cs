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
        private ISupplierRFQRepository _supplierRFQ;

        private ISupplierRFQItemRepository _supplierRFQItem;

        private IRFQSupplierMappingRepository _rfqSupplierMapping;
        private ISupplierQuotationRepository _supplierQuotation;
        private ISupplierQuotationItemRepository _supplierQuotationItem;
        private ISupplierCatalogRepository _supplierCatalog;
        private IRFQQuestionAnswerRepository _rfqQuestionAnswer;
        private IRFQQuestionAnswerOptionRepository _rfqQuestionAnswerOption;
        private ICatalogAssetMappingRepository _catalogAssetMapping;
        private ISupplierVerificationAnswerRepository _supplierVerificationAnswer;
        private ISupplierVerificationAnswerOptionRepository _supplierVerificationAnswerOption;
        private ISupplierCategoryRepository _supplierCategory;


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
        public ISupplierRFQRepository SupplierRFQ
        {
            get
            {
                if (_supplierRFQ == null)
                {
                    _supplierRFQ = new SupplierRFQRepository(_context);
                }
                return _supplierRFQ;
            }
        }
        public ISupplierRFQItemRepository SupplierRFQItem
        {
            get
            {
                if (_supplierRFQItem == null)
                {
                    _supplierRFQItem = new SupplierRFQItemRepository(_context);
                }
                return _supplierRFQItem;
            }
        }
        public IRFQSupplierMappingRepository RFQSupplierMapping
        {
            get
            {
                if (_rfqSupplierMapping == null)
                {
                    _rfqSupplierMapping = new RFQSupplierMappingRepository(_context);
                }
                return _rfqSupplierMapping;
            }
        }
        public ISupplierQuotationRepository SupplierQuotation
        {
            get
            {
                if (_supplierQuotation == null)
                {
                    _supplierQuotation = new SupplierQuotationRepository(_context);
                }
                return _supplierQuotation;
            }
        }
        public ISupplierQuotationItemRepository SupplierQuotationItem
        {
            get
            {
                if (_supplierQuotationItem == null)
                {
                    _supplierQuotationItem = new SupplierQuotationItemRepository(_context);
                }
                return _supplierQuotationItem;
            }
        }


        public ISupplierCatalogRepository SupplierCatalog
        {
            get
            {
                if (_supplierCatalog == null)
                {
                    _supplierCatalog =
                        new SupplierCatalogRepository(_context);
                }

                return _supplierCatalog;
            }
        }

        public ICatalogAssetMappingRepository CatalogAssetMapping
        {
            get
            {
                if (_catalogAssetMapping == null)
                {
                    _catalogAssetMapping =
                        new CatalogAssetMappingRepository(_context);
                }

                return _catalogAssetMapping;
            }
        }
        public ISupplierVerificationAnswerRepository SupplierVerificationAnswer
        {
            get
            {
                if (_supplierVerificationAnswer == null)
                {
                    _supplierVerificationAnswer =
                        new SupplierVerificationAnswerRepository(_context);
                }

                return _supplierVerificationAnswer;
            }
        }

        public IRFQQuestionAnswerRepository RFQQuestionAnswer
        {
            get
            {
                if (_rfqQuestionAnswer == null)
                {
                    _rfqQuestionAnswer =
                        new RFQQuestionAnswerRepository(_context);
                }

                return _rfqQuestionAnswer;
            }
        }
        public IRFQQuestionAnswerOptionRepository RFQQuestionAnswerOption
        {
            get
            {
                if (_rfqQuestionAnswerOption == null)
                {
                    _rfqQuestionAnswerOption =
                        new RFQQuestionAnswerOptionRepository(_context);
                }

                return _rfqQuestionAnswerOption;
            }
        }

        public ISupplierCategoryRepository SupplierCategory
        {
            get
            {
                if (_supplierCategory == null)
                {
                    _supplierCategory = new SupplierCategoryRepository(_context);
                }
                return _supplierCategory;
            }
        }

        public ISupplierVerificationAnswerOptionRepository SupplierVerificationAnswerOption
        {
            get
            {
                if (_supplierVerificationAnswerOption == null)
                {
                    _supplierVerificationAnswerOption = new SupplierVerificationAnswerOptionRepository(_context);
                }

                return _supplierVerificationAnswerOption;
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