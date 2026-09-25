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
        private IRFQOrganizationUserMappingRepository _rfqOrganizationUserMapping;

        private ISupplierVerificationRequestRepository _supplierVerificationRequest;
        private IVerificationTemplateRepository _verificationTemplateRepository;
        private IVerificationTemplateQuestionRepository _verificationTemplateQuestionRepository;
        private IVerificationTemplateQuestionOptionRepository _verificationTemplateQuestionOptionRepository;
        private IDefaultVerificationTemplateQuestionRepository _defaultVerificationTemplateQuestionRepository;
        private IDefaultVerificationTemplateRepository _defaultVerificationTemplateRepository;

        private IRFQQuestionAttachmentMappingRepository _rfqQuestionAttachmentMappingRepository;
        private IRFQBlockchainRecordRepository _rfqBlockchainRecordRepository;
        private IExternalSupplierRepository _externalSupplierRepository;
        private IRFQExternalSupplierRepository _rfqExternalSupplierRepository;
        private IMessageThreadRepository _messageThreadRepository;
        private IMessageRepository _messageRepository;
        private IMessageAttachmentRepository _messageAttachmentRepository;
        private IPredefinedMaterialRepository _predefinedMaterialRepository;
        private IApprovalFlowUserMappingRepository _approvalFlowUserMappingRepository;
        private IApprovalFlowPredefinedMaterialMappingRepository _approvalFlowPredefinedMaterialMappingRepository;
        private IPredefinedMaterialApprovalFlowUserMappingRepository _predefinedMaterialApprovalFlowUserMappingRepository;
        private IMasterApprovalFlowRepository _masterApprovalFlowRepository;
        private IExcelMaterialMasterRepository _excelMaterialMasterRepository;
        private IRFQAwardRepository _rfqAwardRepository;
        private IRFQAwardItemRepository _rfqAwardItemRepository;
        private IContractRepository _contractRepository;
        private IContractAttachmentRepository _contractAttachmentRepository;
        private IContractApprovalFlowRepository _contractApprovalFlowRepository;
        private IContractApprovalUserMappingRepository _contractApprovalUserMappingRepository;

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
        public IRFQOrganizationUserMappingRepository RFQOrganizationUserMapping
        {
            get
            {
                if (_rfqOrganizationUserMapping == null)
                {
                    _rfqOrganizationUserMapping =
                    new RFQOrganizationUserMappingRepository(_context);
                }
                return _rfqOrganizationUserMapping;
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

         public IVerificationTemplateQuestionRepository VerificationTemplateQuestion
        {
            get
            {
                if (_verificationTemplateQuestionRepository == null)
                {
                   _verificationTemplateQuestionRepository = new VerificationTemplateQuestionRepository(_context);
                }

                return _verificationTemplateQuestionRepository;
            }
        }
        public IVerificationTemplateQuestionOptionRepository VerificationTemplateQuestionOptionRepository
        {
            get
            {
                if (_verificationTemplateQuestionOptionRepository == null)
                {
                   _verificationTemplateQuestionOptionRepository = new VerificationTemplateQuestionOptionRepository(_context);
                }

                return _verificationTemplateQuestionOptionRepository;
            }
        }
            public IDefaultVerificationTemplateQuestionRepository DefaultVerificationTemplateQuestionRepository
        {
            get
            {
                if (_defaultVerificationTemplateQuestionRepository == null)
                {
                   _defaultVerificationTemplateQuestionRepository = new DefaultVerificationTemplateQuestionRepository(_context);
                }

                return _defaultVerificationTemplateQuestionRepository;
            }
        }

         public IDefaultVerificationTemplateRepository DefaultVerificationTemplateRepository
        {
            get
            {
                if (_defaultVerificationTemplateRepository == null)
                {
                   _defaultVerificationTemplateRepository = new DefaultVerificationTemplateRepository(_context);
                }

                return _defaultVerificationTemplateRepository;
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
        public IRFQBlockchainRecordRepository RFQBlockchainRecord
        {
            get
            {
                if (_rfqBlockchainRecordRepository == null)
                {
                    _rfqBlockchainRecordRepository = new RFQBlockchainRecordRepository(_context);
                }

                return _rfqBlockchainRecordRepository;
            }
        }
        public IExternalSupplierRepository ExternalSupplier
        {
            get
            {
                if (_externalSupplierRepository == null)
                {
                    _externalSupplierRepository = new ExternalSupplierRepository(_context);
                }

                return _externalSupplierRepository;
            }
        }
        public IRFQExternalSupplierRepository RFQExternalSupplier
        {
            get
            {
                if (_rfqExternalSupplierRepository == null)
                {
                    _rfqExternalSupplierRepository = new RFQExternalSupplierRepository(_context);
                }

                return _rfqExternalSupplierRepository;
            }
        }
        public IMessageThreadRepository MessageThread
        {
            get
            {
                if (_messageThreadRepository == null)
                {
                    _messageThreadRepository = new MessageThreadRepository(_context);
                }

                return _messageThreadRepository;
            }
        }
        public IMessageRepository Message
        {
            get
            {
                if (_messageRepository == null)
                {
                    _messageRepository = new MessageRepository(_context);
                }

                return _messageRepository;
            }
        }
        public IMessageAttachmentRepository MessageAttachment
        {
            get
            {
                if (_messageAttachmentRepository == null)
                {
                    _messageAttachmentRepository = new MessageAttachmentRepository(_context);
                }

                return _messageAttachmentRepository;
            }
        }
        public IPredefinedMaterialRepository PredefinedMaterial
        {
            get
            {
                if (_predefinedMaterialRepository == null)
                {
                    _predefinedMaterialRepository = new PredefinedMaterialRepository(_context);
                }

                return _predefinedMaterialRepository;
            }
        }
        public IApprovalFlowUserMappingRepository ApprovalFlowUserMapping
        {
            get
            {
                if (_approvalFlowUserMappingRepository == null)
                {
                    _approvalFlowUserMappingRepository = new ApprovalFlowUserMappingRepository(_context);
                }

                return _approvalFlowUserMappingRepository;
            }
        }
        public IApprovalFlowPredefinedMaterialMappingRepository ApprovalFlowPredefinedMaterialMapping
        {
            get
            {
                if (_approvalFlowPredefinedMaterialMappingRepository == null)
                {
                    _approvalFlowPredefinedMaterialMappingRepository = new ApprovalFlowPredefinedMaterialMappingRepository(_context);
                }

                return _approvalFlowPredefinedMaterialMappingRepository;
            }
        }
        public IPredefinedMaterialApprovalFlowUserMappingRepository PredefinedMaterialApprovalFlowUserMapping
        {
            get
            {
                if (_predefinedMaterialApprovalFlowUserMappingRepository == null)
                {
                    _predefinedMaterialApprovalFlowUserMappingRepository = new PredefinedMaterialApprovalFlowUserMappingRepository(_context);
                }

                return _predefinedMaterialApprovalFlowUserMappingRepository;
            }
        }
        public IMasterApprovalFlowRepository MasterApprovalFlow
        {
            get
            {
                if (_masterApprovalFlowRepository == null)
                {
                    _masterApprovalFlowRepository = new MasterApprovalFlowRepositoty(_context);
                }

                return _masterApprovalFlowRepository;
            }
        }
        public IExcelMaterialMasterRepository ExcelMaterialMaster
        {
            get
            {
                if (_excelMaterialMasterRepository == null)
                {
                    _excelMaterialMasterRepository = new ExcelMaterialMasterRepository(_context);
                }

                return _excelMaterialMasterRepository;
            }
        }
        public IRFQAwardRepository RFQAward
        {
            get
            {
                if (_rfqAwardRepository == null)
                {
                    _rfqAwardRepository = new RFQAwardRepository(_context);
                }
                return _rfqAwardRepository;
            }
        }
        public IRFQAwardItemRepository RFQAwardItem
        {
            get
            {
                if (_rfqAwardItemRepository == null)
                {
                    _rfqAwardItemRepository = new RFQAwardItemRepository(_context);
                }
                return _rfqAwardItemRepository;
            }
        }
        public IContractRepository Contract
        {
            get
            {
                if (_contractRepository == null)
                {
                    _contractRepository = new ContractRepository(_context);
                }
                return _contractRepository;
            }
        }
        public IContractAttachmentRepository ContractAttachment
        {
            get
            {
                if (_contractAttachmentRepository == null)
                {
                    _contractAttachmentRepository = new ContractAttachmentRepository(_context);
                }
                return _contractAttachmentRepository;
            }
        }
        public IContractApprovalFlowRepository ContractApprovalFlow
        {
            get
            {
                if (_contractApprovalFlowRepository == null)
                {
                    _contractApprovalFlowRepository = new ContractApprovalFlowRepository(_context);
                }
                return _contractApprovalFlowRepository;
            }
        }
        public IContractApprovalUserMappingRepository ContractApprovalUserMapping
        {
            get
            {
                if (_contractApprovalUserMappingRepository == null)
                {
                    _contractApprovalUserMappingRepository = new ContractApprovalUserMappingRepository(_context);
                }
                return _contractApprovalUserMappingRepository;
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
