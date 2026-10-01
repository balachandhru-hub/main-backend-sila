using Microsoft.EntityFrameworkCore.Storage;

namespace Operations.Infrastructure.Contracts.IRepository
{
    /// <summary>
    /// Repository Wrapper class holding every instance of repository.
    /// </summary>
    public interface IRepositoryWrapper
    {
        IOrganizationUnitRepository OrganizationUnit { get; }
        IDocumentStorageConnectionRepository DocumentStorageConnection { get; }
        IDocumentStorageDestinationRepository DocumentStorageDestination { get; }
        IUserDocumentStorageAssignmentRepository UserDocumentStorageAssignment { get; }
        IMicrosoftAuthorizationStateRepository MicrosoftAuthorizationState { get; }
        ISupplierMasterRepository SupplierMaster { get; }
        ISupplierAliasRepository SupplierAlias { get; }
        IMaterialRepository Material { get; }
        ISupplierMaterialRepository SupplierMaterial { get; }
        IDocumentRepository Document { get; }
        IDocumentPageRepository DocumentPage { get; }
        IDocumentExtractionRepository DocumentExtraction { get; }
        IDocumentTransferJobRepository DocumentTransferJob { get; }
        IInvoiceExtDataRepository InvoiceExtData { get; }
        IExtractionAgentConfigRepository ExtractionAgentConfig { get; }
        IInvoiceOcrConfigurationRepository InvoiceOcrConfiguration { get; }
        IInvoiceRepository Invoice { get; }
        IInvoiceLineRepository InvoiceLine { get; }
        IPurchaseOrderRepository PurchaseOrder { get; }
        IPurchaseOrderItemRepository PurchaseOrderItem { get; }
        IGoodsReceiptRepository GoodsReceipt { get; }
        IGoodsReceiptLineRepository GoodsReceiptLine { get; }
        IStockBalanceRepository StockBalance { get; }
        IInventoryTransactionRepository InventoryTransaction { get; }
        IIdempotencyRecordRepository IdempotencyRecord { get; }
        IAuditEventRepository AuditEvent { get; }
        IApiIntegrationConfigurationRepository ApiIntegrationConfiguration { get; }
        IIntegrationSchemaSnapshotRepository IntegrationSchemaSnapshot { get; }
        IApiFieldMappingRepository ApiFieldMapping { get; }
        IApiIntegrationExecutionRepository ApiIntegrationExecution { get; }

        bool Save();
        Task<bool> SaveAsync();

        /// <summary>
        /// Opens a database transaction. Used only by goods receipt posting, where the lock on the
        /// purchase order items must be held from the open-quantity check until the stock is updated.
        /// </summary>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    }
}
