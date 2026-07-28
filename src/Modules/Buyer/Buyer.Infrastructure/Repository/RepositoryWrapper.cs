using Buyer.Infrastructure.DbContext;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;
using Buyer.Infrastructure.Contracts.IServices;
using Buyer.Infrastructure.Contracts.IRepository;


namespace Buyer.Infrastructure.Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly RepositoryContext _context;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private readonly string _dbConnectionString;
        private IBuyerBusinessProfileRepository _buyerBusinessProfileRepository;
        private IBuyerCategoryRepository _buyerCategoryRepository;
        private IAssetRepository _assetRepository;
        private IBuyerBankAccountRepository _buyerBankAccountRepository;
        private IBuyerDeliveryLocationRepository _buyerDeliveryLocationRepository;
        private IBuyerRegistrationRepository _buyerRegistrationRepository;
        private IItemBuyerMasterRepository _itemBuyerMasterRepository;
        private IBulkInsertHelper? _bulkInsertHelper;
        private IBuyerDepartmentRepository _buyerDepartmentRepository;
        private IBuyerCostCenterRepository _buyerCostCenterRepository;
        private IRFQRepository _rfqRepository;
        private IRFQAttachmentMappingRepository _rfqAttachmentMappingRepository;
        private IRFQQuestionRepository _rfqQuestionRepository;
        private IRFQQuestionOptionRepository _rfqQuestionOptionRepository;
        private IRFQItemRepository _rfqItemRepository;
        private IBuyerSupplierMappingRepository _buyerSupplierMappingRepository;

        private IRFQItemAttachmentMappingRepository _rfqItemAttachmentMappingRepository;
        private IRFQSupplierMappingRepository _rfqSupplierMapping;

        private ISupplierVerificationRequestRepository _supplierVerificationRequest;
        private IVerificationTemplateRepository _verificationTemplateRepository;
        private IRFQQuestionAttachmentMappingRepository _rfqQuestionAttachmentMappingRepository;

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
                    _buyerBusinessProfileRepository = new BuyerBusinessProfileRepository(_context);
                }
                return _buyerBusinessProfileRepository;
            }
        }
        public IBuyerBankAccountRepository BuyerBankAccount
        {
            get
            {
                if (_buyerBankAccountRepository == null)
                {
                    _buyerBankAccountRepository = new BuyerBankAccountRepository(_context);
                }
                return _buyerBankAccountRepository;
            }
        }
        public IAssetRepository Asset
        {
            get
            {
                if (_assetRepository == null)
                {
                    _assetRepository = new AssetRepository(_context);
                }
                return _assetRepository;
            }
        }
        public IBuyerCategoryRepository BuyerCategory
        {
            get
            {
                if (_buyerCategoryRepository == null)
                {
                    _buyerCategoryRepository = new BuyerCategoryRepository(_context);
                }
                return _buyerCategoryRepository;
            }
        }
        public IBuyerDeliveryLocationRepository BuyerDeliveryLocation
        {
            get
            {
                if (_buyerDeliveryLocationRepository == null)
                {
                    _buyerDeliveryLocationRepository = new BuyerDeliveryLocationRepository(_context);
                }
                return _buyerDeliveryLocationRepository;
            }
        }
        public IBuyerRegistrationRepository BuyerRegistration
        {
            get
            {
                if (_buyerRegistrationRepository == null)
                {
                    _buyerRegistrationRepository = new BuyerRegistrationRepository(_context);
                }
                return _buyerRegistrationRepository;
            }
        }
        public IBuyerDepartmentRepository BuyerDepartment
        {
            get
            {
                if (_buyerDepartmentRepository == null)
                {
                    _buyerDepartmentRepository = new BuyerDepartmentRepository(_context);
                }
                return _buyerDepartmentRepository;
            }
        }
        public IBuyerCostCenterRepository BuyerCostCenter
        {
            get
            {
                if (_buyerCostCenterRepository == null)
                {
                    _buyerCostCenterRepository = new BuyerCostCenterRepository(_context);
                }
                return _buyerCostCenterRepository;
            }
        }
        public IRFQRepository RFQ
        {
            get
            {
                if (_rfqRepository == null)
                {
                    _rfqRepository = new RFQRepository(_context);
                }

                return _rfqRepository;
            }
        }
        public IRFQAttachmentMappingRepository RFQAttachmentMapping
        {
            get
            {
                if (_rfqAttachmentMappingRepository == null)
                {
                    _rfqAttachmentMappingRepository =
                        new RFQAttachmentMappingRepository(_context);
                }

                return _rfqAttachmentMappingRepository;
            }
        }
        public IRFQQuestionRepository RFQQuestion
        {
            get
            {
                if (_rfqQuestionRepository == null)
                {
                    _rfqQuestionRepository = new RFQQuestionRepository(_context);
                }

                return _rfqQuestionRepository;
            }
        }

        public IRFQQuestionOptionRepository RFQQuestionOption
        {
            get
            {
                if (_rfqQuestionOptionRepository == null)
                {
                    _rfqQuestionOptionRepository = new RFQQuestionOptionRepository(_context);
                }

                return _rfqQuestionOptionRepository;
            }
        }
        public IRFQItemRepository RFQItem
        {
            get
            {
                if (_rfqItemRepository == null)
                {
                    _rfqItemRepository = new RFQItemRepository(_context);
                }

                return _rfqItemRepository;
            }
        }

        public IRFQItemAttachmentMappingRepository RFQItemAttachmentMapping
        {
            get
            {
                if (_rfqItemAttachmentMappingRepository == null)
                {
                    _rfqItemAttachmentMappingRepository =
                        new RFQItemAttachmentMappingRepository(_context);
                }

                return _rfqItemAttachmentMappingRepository;
            }
        }
        public IBuyerSupplierMappingRepository BuyerSupplierMapping
        {
            get
            {
                if(_buyerSupplierMappingRepository==null)
                {
                    _buyerSupplierMappingRepository=
                    new BuyerSupplierMappingRepository(_context);
                }
                return _buyerSupplierMappingRepository;
            }
        }
         public IRFQSupplierMappingRepository RFQSupplierMapping
        {
            get
            {
                if( _rfqSupplierMapping==null)
                {
                     _rfqSupplierMapping=
                    new RFQSupplierMappingRepository(_context);
                }
                return  _rfqSupplierMapping;
            }
        }
         public ISupplierVerificationRequestRepository SupplierVerificationRequest
        {
            get
            {
                if( _supplierVerificationRequest==null)
                {
                     _supplierVerificationRequest=
                    new SupplierVerificationRequestRepository(_context);
                }
                return  _supplierVerificationRequest;
            }
        }


        public IItemBuyerMasterRepository ItemBuyerMaster
        {
            get
            {
                if (_itemBuyerMasterRepository == null)
                {
                    _itemBuyerMasterRepository =
                        new ItemBuyerMasterRepository(_context);
                }

                return _itemBuyerMasterRepository;
            }
        }
        public IBulkInsertHelper BulkInsertHelper
        {
            get
            {
                if (_bulkInsertHelper == null)
                {
                    _bulkInsertHelper = new BulkInsertHelper(
                        _context,
                        _userIdentityService);
                }

                return _bulkInsertHelper;
            }
        }
         public IVerificationTemplateRepository VerificationTemplate
        {
            get
            {
                if (_verificationTemplateRepository == null)
                {
                   _verificationTemplateRepository = new VerificationTemplateRepository(_context);
                }

                return _verificationTemplateRepository;
            }
        }
        public IRFQQuestionAttachmentMappingRepository RFQQuestionAttachmentMapping
        {
            get
            {
                if (_rfqQuestionAttachmentMappingRepository == null)
                {
                    _rfqQuestionAttachmentMappingRepository = new RFQQuestionAttachmentMappingRepository(_context);
                }

                return _rfqQuestionAttachmentMappingRepository;
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
