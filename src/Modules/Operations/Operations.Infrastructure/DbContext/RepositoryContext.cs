using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using SharedKernel.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using SharedKernel.Util;
using Operations.Domain.Common;

namespace Operations.Infrastructure.DbContext
{
    public class RepositoryContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IConfiguration _configuration;

        public RepositoryContext(DbContextOptions<RepositoryContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }
        public DbSet<OrganizationUnit> OrganizationUnit { get; set; }
        public DbSet<DocumentStorageConnection> DocumentStorageConnection { get; set; }
        public DbSet<DocumentStorageDestination> DocumentStorageDestination { get; set; }
        public DbSet<UserDocumentStorageAssignment> UserDocumentStorageAssignment { get; set; }
        public DbSet<MicrosoftAuthorizationState> MicrosoftAuthorizationState { get; set; }
        public DbSet<SupplierMaster> SupplierMaster { get; set; }
        public DbSet<SupplierAlias> SupplierAlias { get; set; }
        public DbSet<Material> Material { get; set; }
        public DbSet<SupplierMaterial> SupplierMaterial { get; set; }
        public DbSet<Document> Document { get; set; }
        public DbSet<DocumentPage> DocumentPage { get; set; }
        public DbSet<DocumentExtraction> DocumentExtraction { get; set; }
        public DbSet<DocumentTransferJob> DocumentTransferJob { get; set; }
        public DbSet<InvoiceExtData> InvoiceExtData { get; set; }
        public DbSet<ExtractionAgentConfig> ExtractionAgentConfig { get; set; }
        public DbSet<InvoiceOcrConfiguration> InvoiceOcrConfiguration { get; set; }
        public DbSet<Invoice> Invoice { get; set; }
        public DbSet<InvoiceLine> InvoiceLine { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrder { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItem { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipt { get; set; }
        public DbSet<GoodsReceiptLine> GoodsReceiptLine { get; set; }
        public DbSet<StockBalance> StockBalance { get; set; }
        public DbSet<InventoryTransaction> InventoryTransaction { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecord { get; set; }
        public DbSet<AuditEvent> AuditEvent { get; set; }
        public DbSet<ApiIntegrationConfiguration> ApiIntegrationConfiguration { get; set; }
        public DbSet<IntegrationSchemaSnapshot> IntegrationSchemaSnapshot { get; set; }
        public DbSet<ApiFieldMapping> ApiFieldMapping { get; set; }
        public DbSet<ApiIntegrationExecution> ApiIntegrationExecution { get; set; }


        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            _ = modelBuilder.HasDefaultSchema(_configuration[Common.APPLICATION_SCHEMA]);
            _ = modelBuilder.Entity<OrganizationUnit>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<OrganizationUnit>().HasIndex(a => new { a.OrganizationId, a.Code }).IsUnique();
            _ = modelBuilder.Entity<DocumentStorageConnection>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<DocumentStorageConnection>().HasIndex(a => new { a.OrganizationId, a.Provider, a.Name }).IsUnique();
            _ = modelBuilder.Entity<DocumentStorageDestination>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<DocumentStorageDestination>().HasIndex(a => new { a.OrganizationId, a.StorageConnectionId, a.DocumentType, a.OperatingUnitId }).IsUnique();
            _ = modelBuilder.Entity<UserDocumentStorageAssignment>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<UserDocumentStorageAssignment>().HasIndex(a => a.UserId).IsUnique();
            _ = modelBuilder.Entity<MicrosoftAuthorizationState>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<MicrosoftAuthorizationState>().HasIndex(a => a.StateHash).IsUnique();
            _ = modelBuilder.Entity<SupplierMaster>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SupplierMaster>().HasIndex(a => new { a.OrganizationId, a.EntityCode, a.SupplierCode }).IsUnique();
            _ = modelBuilder.Entity<SupplierMaster>().HasIndex(a => new { a.OrganizationId, a.NormalizedName });
            _ = modelBuilder.Entity<SupplierAlias>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SupplierAlias>().HasIndex(a => new { a.OrganizationId, a.EntityCode, a.NormalizedAlias }).IsUnique();
            _ = modelBuilder.Entity<Material>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Material>().HasIndex(a => new { a.OrganizationId, a.MaterialCode }).IsUnique();
            _ = modelBuilder.Entity<SupplierMaterial>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Document>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Document>().HasIndex(a => new { a.OrganizationId, a.Status });
            _ = modelBuilder.Entity<Document>().HasIndex(a => new { a.OrganizationId, a.ScanSessionId }).IsUnique();
            _ = modelBuilder.Entity<Document>().HasIndex(a => new { a.OrganizationId, a.ContentHash });
            _ = modelBuilder.Entity<DocumentPage>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<DocumentPage>().HasIndex(a => new { a.DocumentId, a.PageNumber }).IsUnique();
            _ = modelBuilder.Entity<DocumentExtraction>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<DocumentExtraction>().HasIndex(a => new { a.DocumentId, a.ExtractionType });
            _ = modelBuilder.Entity<DocumentTransferJob>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<DocumentTransferJob>().HasIndex(a => new { a.DocumentId, a.DestinationId }).IsUnique();
            _ = modelBuilder.Entity<DocumentTransferJob>().HasIndex(a => new { a.Status, a.NextAttemptAt });
            _ = modelBuilder.Entity<InvoiceExtData>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InvoiceExtData>().HasIndex(a => a.OrganizationId);
            _ = modelBuilder.Entity<ExtractionAgentConfig>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ExtractionAgentConfig>().HasIndex(a => new { a.OrganizationId, a.DocumentType });
            _ = modelBuilder.Entity<InvoiceOcrConfiguration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InvoiceOcrConfiguration>().HasIndex(a => a.OrganizationId).IsUnique();
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => new { a.OrganizationId, a.InvoiceNumber });
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => new { a.OrganizationId, a.Status });
            _ = modelBuilder.Entity<InvoiceLine>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InvoiceLine>().HasIndex(a => new { a.InvoiceId, a.LineNumber });
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => new { a.OrganizationId, a.EntityCode, a.PoNumber }).IsUnique();
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => new { a.OrganizationId, a.SourceConfigurationId, a.ExternalId });
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => new { a.OrganizationId, a.OperatingUnitId, a.Status });
            _ = modelBuilder.Entity<PurchaseOrderItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PurchaseOrderItem>().HasIndex(a => new { a.PurchaseOrderId, a.LineNumber }).IsUnique();
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => new { a.OrganizationId, a.GrnNumber }).IsUnique();
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => new { a.OrganizationId, a.Status });
            _ = modelBuilder.Entity<GoodsReceiptLine>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockBalance>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockBalance>().HasIndex(a => new { a.OrganizationId, a.OperatingUnitId, a.MaterialCode, a.Uom }).IsUnique();
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => a.ReferenceId);
            _ = modelBuilder.Entity<IdempotencyRecord>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<IdempotencyRecord>().HasIndex(a => new { a.OrganizationId, a.UserId, a.IdempotencyKey, a.Operation }).IsUnique();
            _ = modelBuilder.Entity<AuditEvent>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<AuditEvent>().HasIndex(a => new { a.OrganizationId, a.DateCreated });
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().HasIndex(a => new { a.OrganizationId, a.EntityCode, a.ProcessType }).IsUnique();
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().HasIndex(a => new { a.Status, a.NextRunAt });
            _ = modelBuilder.Entity<IntegrationSchemaSnapshot>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiFieldMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiFieldMapping>().HasIndex(a => new { a.ConfigurationId, a.SourceField, a.TargetField }).IsUnique();
            _ = modelBuilder.Entity<ApiIntegrationExecution>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiIntegrationExecution>().HasIndex(a => new { a.ConfigurationId, a.StartedAt });

            base.OnModelCreating(modelBuilder);

            foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            {

                entity.SetTableName(entity.GetTableName()!.ConvertToSnakeCase());
                var storeObjectIdentifier = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableProperty property in entity.GetProperties())
                {

                    property.SetColumnName(property.GetColumnName(storeObjectIdentifier)!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableKey key in entity.GetKeys())
                {
                    key.SetName(key.GetName()!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(key.GetConstraintName()!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableIndex index in entity.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName()!.ConvertToSnakeCase());
                }
            }
        }
        public void OnBeforeSaving(Guid UserId)
        {
            System.Collections.Generic.IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries = ChangeTracker.Entries();
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in entries)
            {
                if (entry.Entity is BaseModel trackable)
                {
                    DateTime now = DateTime.UtcNow;
                    Guid user = UserId;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.DateUpdated = now;
                            trackable.UpdatedBy = user;
                            break;
                        case EntityState.Added:
                            trackable.DateCreated = now;
                            trackable.CreatedBy = user;
                            trackable.DateUpdated = now;
                            trackable.UpdatedBy = user;
                            trackable.IsActive = true;
                            break;
                        case EntityState.Detached:
                            break;
                        case EntityState.Unchanged:
                            break;
                        case EntityState.Deleted:
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
