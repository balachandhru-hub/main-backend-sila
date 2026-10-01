using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.Contracts.IServices;
using Operations.Infrastructure.DbContext;
using SharedKernel.LoggerServices;

namespace Operations.Infrastructure.Repository
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private readonly RepositoryContext _context;
        private readonly IUserIdentityService _userIdentityService;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;
        private IOrganizationUnitRepository? _organizationUnitRepository;
        private IDocumentStorageConnectionRepository? _documentStorageConnectionRepository;
        private IDocumentStorageDestinationRepository? _documentStorageDestinationRepository;
        private IUserDocumentStorageAssignmentRepository? _userDocumentStorageAssignmentRepository;
        private IMicrosoftAuthorizationStateRepository? _microsoftAuthorizationStateRepository;
        private ISupplierMasterRepository? _supplierMasterRepository;
        private ISupplierAliasRepository? _supplierAliasRepository;
        private IMaterialRepository? _materialRepository;
        private ISupplierMaterialRepository? _supplierMaterialRepository;
        private IDocumentRepository? _documentRepository;
        private IDocumentPageRepository? _documentPageRepository;
        private IDocumentExtractionRepository? _documentExtractionRepository;
        private IDocumentTransferJobRepository? _documentTransferJobRepository;
        private IInvoiceExtDataRepository? _invoiceExtDataRepository;
        private IExtractionAgentConfigRepository? _extractionAgentConfigRepository;
        private IInvoiceOcrConfigurationRepository? _invoiceOcrConfigurationRepository;
        private IInvoiceRepository? _invoiceRepository;
        private IInvoiceLineRepository? _invoiceLineRepository;
        private IPurchaseOrderRepository? _purchaseOrderRepository;
        private IPurchaseOrderItemRepository? _purchaseOrderItemRepository;
        private IGoodsReceiptRepository? _goodsReceiptRepository;
        private IGoodsReceiptLineRepository? _goodsReceiptLineRepository;
        private IStockBalanceRepository? _stockBalanceRepository;
        private IInventoryTransactionRepository? _inventoryTransactionRepository;
        private IIdempotencyRecordRepository? _idempotencyRecordRepository;
        private IAuditEventRepository? _auditEventRepository;
        private IApiIntegrationConfigurationRepository? _apiIntegrationConfigurationRepository;
        private IIntegrationSchemaSnapshotRepository? _integrationSchemaSnapshotRepository;
        private IApiFieldMappingRepository? _apiFieldMappingRepository;
        private IApiIntegrationExecutionRepository? _apiIntegrationExecutionRepository;

        public RepositoryWrapper(RepositoryContext repositoryContext, IUserIdentityService userIdentityService, IConfiguration configuration, ILoggerManager logger)
        {
            _context = repositoryContext;
            _userIdentityService = userIdentityService;
            _logger = logger;
            _configuration = configuration;
        }
        public IOrganizationUnitRepository OrganizationUnit
        {
            get
            {
                if (_organizationUnitRepository == null)
                {
                    _organizationUnitRepository = new OrganizationUnitRepository(_context);
                }
                return _organizationUnitRepository;
            }
        }
        public IDocumentStorageConnectionRepository DocumentStorageConnection
        {
            get
            {
                if (_documentStorageConnectionRepository == null)
                {
                    _documentStorageConnectionRepository = new DocumentStorageConnectionRepository(_context);
                }
                return _documentStorageConnectionRepository;
            }
        }
        public IDocumentStorageDestinationRepository DocumentStorageDestination
        {
            get
            {
                if (_documentStorageDestinationRepository == null)
                {
                    _documentStorageDestinationRepository = new DocumentStorageDestinationRepository(_context);
                }
                return _documentStorageDestinationRepository;
            }
        }
        public IUserDocumentStorageAssignmentRepository UserDocumentStorageAssignment
        {
            get
            {
                if (_userDocumentStorageAssignmentRepository == null)
                {
                    _userDocumentStorageAssignmentRepository = new UserDocumentStorageAssignmentRepository(_context);
                }
                return _userDocumentStorageAssignmentRepository;
            }
        }
        public IMicrosoftAuthorizationStateRepository MicrosoftAuthorizationState
        {
            get
            {
                if (_microsoftAuthorizationStateRepository == null)
                {
                    _microsoftAuthorizationStateRepository = new MicrosoftAuthorizationStateRepository(_context);
                }
                return _microsoftAuthorizationStateRepository;
            }
        }
        public ISupplierMasterRepository SupplierMaster
        {
            get
            {
                if (_supplierMasterRepository == null)
                {
                    _supplierMasterRepository = new SupplierMasterRepository(_context);
                }
                return _supplierMasterRepository;
            }
        }
        public ISupplierAliasRepository SupplierAlias
        {
            get
            {
                if (_supplierAliasRepository == null)
                {
                    _supplierAliasRepository = new SupplierAliasRepository(_context);
                }
                return _supplierAliasRepository;
            }
        }
        public IMaterialRepository Material
        {
            get
            {
                if (_materialRepository == null)
                {
                    _materialRepository = new MaterialRepository(_context);
                }
                return _materialRepository;
            }
        }
        public ISupplierMaterialRepository SupplierMaterial
        {
            get
            {
                if (_supplierMaterialRepository == null)
                {
                    _supplierMaterialRepository = new SupplierMaterialRepository(_context);
                }
                return _supplierMaterialRepository;
            }
        }
        public IDocumentRepository Document
        {
            get
            {
                if (_documentRepository == null)
                {
                    _documentRepository = new DocumentRepository(_context);
                }
                return _documentRepository;
            }
        }
        public IDocumentPageRepository DocumentPage
        {
            get
            {
                if (_documentPageRepository == null)
                {
                    _documentPageRepository = new DocumentPageRepository(_context);
                }
                return _documentPageRepository;
            }
        }
        public IDocumentExtractionRepository DocumentExtraction
        {
            get
            {
                if (_documentExtractionRepository == null)
                {
                    _documentExtractionRepository = new DocumentExtractionRepository(_context);
                }
                return _documentExtractionRepository;
            }
        }
        public IDocumentTransferJobRepository DocumentTransferJob
        {
            get
            {
                if (_documentTransferJobRepository == null)
                {
                    _documentTransferJobRepository = new DocumentTransferJobRepository(_context);
                }
                return _documentTransferJobRepository;
            }
        }
        public IInvoiceExtDataRepository InvoiceExtData
        {
            get
            {
                if (_invoiceExtDataRepository == null)
                {
                    _invoiceExtDataRepository = new InvoiceExtDataRepository(_context);
                }
                return _invoiceExtDataRepository;
            }
        }
        public IExtractionAgentConfigRepository ExtractionAgentConfig
        {
            get
            {
                if (_extractionAgentConfigRepository == null)
                {
                    _extractionAgentConfigRepository = new ExtractionAgentConfigRepository(_context);
                }
                return _extractionAgentConfigRepository;
            }
        }
        public IInvoiceOcrConfigurationRepository InvoiceOcrConfiguration
        {
            get
            {
                if (_invoiceOcrConfigurationRepository == null)
                {
                    _invoiceOcrConfigurationRepository = new InvoiceOcrConfigurationRepository(_context);
                }
                return _invoiceOcrConfigurationRepository;
            }
        }
        public IInvoiceRepository Invoice
        {
            get
            {
                if (_invoiceRepository == null)
                {
                    _invoiceRepository = new InvoiceRepository(_context);
                }
                return _invoiceRepository;
            }
        }
        public IInvoiceLineRepository InvoiceLine
        {
            get
            {
                if (_invoiceLineRepository == null)
                {
                    _invoiceLineRepository = new InvoiceLineRepository(_context);
                }
                return _invoiceLineRepository;
            }
        }
        public IPurchaseOrderRepository PurchaseOrder
        {
            get
            {
                if (_purchaseOrderRepository == null)
                {
                    _purchaseOrderRepository = new PurchaseOrderRepository(_context);
                }
                return _purchaseOrderRepository;
            }
        }
        public IPurchaseOrderItemRepository PurchaseOrderItem
        {
            get
            {
                if (_purchaseOrderItemRepository == null)
                {
                    _purchaseOrderItemRepository = new PurchaseOrderItemRepository(_context);
                }
                return _purchaseOrderItemRepository;
            }
        }
        public IGoodsReceiptRepository GoodsReceipt
        {
            get
            {
                if (_goodsReceiptRepository == null)
                {
                    _goodsReceiptRepository = new GoodsReceiptRepository(_context);
                }
                return _goodsReceiptRepository;
            }
        }
        public IGoodsReceiptLineRepository GoodsReceiptLine
        {
            get
            {
                if (_goodsReceiptLineRepository == null)
                {
                    _goodsReceiptLineRepository = new GoodsReceiptLineRepository(_context);
                }
                return _goodsReceiptLineRepository;
            }
        }
        public IStockBalanceRepository StockBalance
        {
            get
            {
                if (_stockBalanceRepository == null)
                {
                    _stockBalanceRepository = new StockBalanceRepository(_context);
                }
                return _stockBalanceRepository;
            }
        }
        public IInventoryTransactionRepository InventoryTransaction
        {
            get
            {
                if (_inventoryTransactionRepository == null)
                {
                    _inventoryTransactionRepository = new InventoryTransactionRepository(_context);
                }
                return _inventoryTransactionRepository;
            }
        }
        public IIdempotencyRecordRepository IdempotencyRecord
        {
            get
            {
                if (_idempotencyRecordRepository == null)
                {
                    _idempotencyRecordRepository = new IdempotencyRecordRepository(_context);
                }
                return _idempotencyRecordRepository;
            }
        }
        public IAuditEventRepository AuditEvent
        {
            get
            {
                if (_auditEventRepository == null)
                {
                    _auditEventRepository = new AuditEventRepository(_context);
                }
                return _auditEventRepository;
            }
        }
        public IApiIntegrationConfigurationRepository ApiIntegrationConfiguration
        {
            get
            {
                if (_apiIntegrationConfigurationRepository == null)
                {
                    _apiIntegrationConfigurationRepository = new ApiIntegrationConfigurationRepository(_context);
                }
                return _apiIntegrationConfigurationRepository;
            }
        }
        public IIntegrationSchemaSnapshotRepository IntegrationSchemaSnapshot
        {
            get
            {
                if (_integrationSchemaSnapshotRepository == null)
                {
                    _integrationSchemaSnapshotRepository = new IntegrationSchemaSnapshotRepository(_context);
                }
                return _integrationSchemaSnapshotRepository;
            }
        }
        public IApiFieldMappingRepository ApiFieldMapping
        {
            get
            {
                if (_apiFieldMappingRepository == null)
                {
                    _apiFieldMappingRepository = new ApiFieldMappingRepository(_context);
                }
                return _apiFieldMappingRepository;
            }
        }
        public IApiIntegrationExecutionRepository ApiIntegrationExecution
        {
            get
            {
                if (_apiIntegrationExecutionRepository == null)
                {
                    _apiIntegrationExecutionRepository = new ApiIntegrationExecutionRepository(_context);
                }
                return _apiIntegrationExecutionRepository;
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

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            return _context.Database.BeginTransactionAsync(cancellationToken);
        }
    }
}
