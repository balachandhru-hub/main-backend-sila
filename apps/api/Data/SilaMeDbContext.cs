using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Data;

public sealed partial class SilaMeDbContext(DbContextOptions<SilaMeDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserApplicationAccess> UserApplicationAccess => Set<UserApplicationAccess>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationUnit> OrganizationUnits => Set<OrganizationUnit>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserOrganizationMembership> UserOrganizationMemberships => Set<UserOrganizationMembership>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<UserAuthorizationOverride> UserAuthorizationOverrides => Set<UserAuthorizationOverride>();
    public DbSet<DocumentStorageConnection> DocumentStorageConnections => Set<DocumentStorageConnection>();
    public DbSet<DocumentStorageDestination> DocumentStorageDestinations => Set<DocumentStorageDestination>();
    public DbSet<UserDocumentStorageAssignment> UserDocumentStorageAssignments => Set<UserDocumentStorageAssignment>();
    public DbSet<MicrosoftAuthorizationState> MicrosoftAuthorizationStates => Set<MicrosoftAuthorizationState>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierAlias> SupplierAliases => Set<SupplierAlias>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<SupplierMaterial> SupplierMaterials => Set<SupplierMaterial>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentPage> DocumentPages => Set<DocumentPage>();
    public DbSet<DocumentExtraction> DocumentExtractions => Set<DocumentExtraction>();
    public DbSet<InvoiceExtData> InvoiceExtData => Set<InvoiceExtData>();
    public DbSet<ExtractionAgentConfig> ExtractionAgentConfigs => Set<ExtractionAgentConfig>();
    public DbSet<InvoiceOcrConfiguration> InvoiceOcrConfigurations => Set<InvoiceOcrConfiguration>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<DocumentTransferJob> DocumentTransferJobs => Set<DocumentTransferJob>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ApiIntegrationConfiguration> ApiIntegrationConfigurations => Set<ApiIntegrationConfiguration>();
    public DbSet<IntegrationSchemaSnapshot> IntegrationSchemaSnapshots => Set<IntegrationSchemaSnapshot>();
    public DbSet<ApiFieldMapping> ApiFieldMappings => Set<ApiFieldMapping>();
    public DbSet<ApiIntegrationExecution> ApiIntegrationExecutions => Set<ApiIntegrationExecution>();
    public DbSet<IntegrationConnectionTest> IntegrationConnectionTests => Set<IntegrationConnectionTest>();
    public DbSet<IntegrationRoute> IntegrationRoutes => Set<IntegrationRoute>();
    public DbSet<IntegrationRouteCompanyCode> IntegrationRouteCompanyCodes => Set<IntegrationRouteCompanyCode>();
    public DbSet<CompanyCodeMaster> CompanyCodes => Set<CompanyCodeMaster>();
    public DbSet<PropertyMaster> Properties => Set<PropertyMaster>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(user => user.FirstName).HasMaxLength(100);
            entity.Property(user => user.LastName).HasMaxLength(100);
            entity.Property(user => user.MobileNumber).HasMaxLength(40);
            entity.Property(user => user.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<UserApplicationAccess>(entity =>
        {
            entity.ToTable("user_application_access");
            entity.HasKey(access => access.Id);
            entity.Property(access => access.Application).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(access => access.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(access => new { access.UserId, access.Application }).IsUnique();
            entity.HasOne(access => access.User)
                .WithMany(user => user.ApplicationAccess)
                .HasForeignKey(access => access.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Application).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(session => session.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(session => session.PlatformUserDisplayName).HasMaxLength(160);
            entity.Property(session => session.PlatformUserEmail).HasMaxLength(320);
            entity.Property(session => session.PlatformRole).HasMaxLength(80);
            entity.Property(session => session.ActorType).HasMaxLength(40);
            entity.Property(session => session.DelegatedPermissionsCsv).HasMaxLength(4000);
            entity.HasIndex(session => session.TokenHash).IsUnique();
            entity.HasIndex(session => new { session.UserId, session.ExpiresAt });
            entity.HasOne(session => session.User)
                .WithMany(user => user.Sessions)
                .HasForeignKey(session => session.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(organization => organization.Id);
            entity.Property(organization => organization.Code).HasMaxLength(64).IsRequired();
            entity.Property(organization => organization.Name).HasMaxLength(200).IsRequired();
            entity.Property(organization => organization.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(organization => organization.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(organization => organization.CustomerLogoFileName).HasMaxLength(260);
            entity.Property(organization => organization.CustomerLogoContentType).HasMaxLength(100);
            entity.HasIndex(organization => organization.Code).IsUnique();
            entity.HasOne(organization => organization.ParentOrganization)
                .WithMany(organization => organization.ChildOrganizations)
                .HasForeignKey(organization => organization.ParentOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationUnit>(entity =>
        {
            entity.ToTable("organization_units");
            entity.HasKey(unit => unit.Id);
            entity.Property(unit => unit.Code).HasMaxLength(64).IsRequired();
            entity.Property(unit => unit.Name).HasMaxLength(200).IsRequired();
            entity.Property(unit => unit.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(unit => unit.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(unit => new { unit.OrganizationId, unit.Code }).IsUnique();
            entity.HasOne(unit => unit.Organization)
                .WithMany(organization => organization.Units)
                .HasForeignKey(unit => unit.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(unit => unit.ParentUnit)
                .WithMany(unit => unit.ChildUnits)
                .HasForeignKey(unit => unit.ParentUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Key).HasMaxLength(64).IsRequired();
            entity.Property(role => role.Name).HasMaxLength(160).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(500);
            entity.Property(role => role.ApplicationScope).HasMaxLength(16).IsRequired();
            entity.Property(role => role.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(role => role.Key).IsUnique();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Key).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.Name).HasMaxLength(160).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(500);
            entity.Property(permission => permission.Module).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.SubModule).HasMaxLength(100);
            entity.Property(permission => permission.ApplicationScope).HasMaxLength(16).IsRequired();
            entity.Property(permission => permission.RiskLevel).HasMaxLength(16).IsRequired();
            entity.HasIndex(permission => permission.Key).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(mapping => new { mapping.RoleId, mapping.PermissionId });
            entity.HasOne(mapping => mapping.Role)
                .WithMany(role => role.Permissions)
                .HasForeignKey(mapping => mapping.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(mapping => mapping.Permission)
                .WithMany(permission => permission.Roles)
                .HasForeignKey(mapping => mapping.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserOrganizationMembership>(entity =>
        {
            entity.ToTable("user_organization_memberships");
            entity.HasKey(membership => membership.Id);
            entity.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(membership => new { membership.UserId, membership.OrganizationId, membership.OrganizationUnitId }).IsUnique();
            entity.HasOne(membership => membership.User)
                .WithMany(user => user.OrganizationMemberships)
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.Organization)
                .WithMany(organization => organization.Memberships)
                .HasForeignKey(membership => membership.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.OrganizationUnit)
                .WithMany(unit => unit.Memberships)
                .HasForeignKey(membership => membership.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserRoleAssignment>(entity =>
        {
            entity.ToTable("user_role_assignments");
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(assignment => new { assignment.UserId, assignment.RoleId, assignment.OrganizationId, assignment.OrganizationUnitId }).IsUnique();
            entity.HasOne(assignment => assignment.User)
                .WithMany(user => user.RoleAssignments)
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(assignment => assignment.Role)
                .WithMany(role => role.UserAssignments)
                .HasForeignKey(assignment => assignment.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(assignment => assignment.Organization)
                .WithMany(organization => organization.RoleAssignments)
                .HasForeignKey(assignment => assignment.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(assignment => assignment.OrganizationUnit)
                .WithMany(unit => unit.RoleAssignments)
                .HasForeignKey(assignment => assignment.OrganizationUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAuthorizationOverride>(entity =>
        {
            entity.ToTable("user_authorization_overrides");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OverrideType).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(500);
            entity.HasIndex(item => new { item.UserId, item.AuthorizationId }).IsUnique();
            entity.HasOne(item => item.User).WithMany(user => user.AuthorizationOverrides).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Authorization).WithMany().HasForeignKey(item => item.AuthorizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentStorageConnection>(entity =>
        {
            entity.ToTable("document_storage_connections");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ConnectionStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.TenantIdentifier).HasMaxLength(250);
            entity.Property(item => item.SiteIdentifier).HasMaxLength(250);
            entity.Property(item => item.DriveIdentifier).HasMaxLength(250);
            entity.Property(item => item.FolderIdentifier).HasMaxLength(250);
            entity.Property(item => item.FolderPath).HasMaxLength(1000);
            entity.Property(item => item.DisplayUrl).HasMaxLength(1000);
            entity.Property(item => item.DisplayName).HasMaxLength(250);
            entity.Property(item => item.DriveName).HasMaxLength(200);
            entity.Property(item => item.CredentialReference).HasColumnType("text");
            entity.Property(item => item.LastTestStatus).HasMaxLength(100);
            entity.HasIndex(item => new { item.OrganizationId, item.Provider, item.Name }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ValidatedByUser).WithMany().HasForeignKey(item => item.ValidatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentStorageDestination>(entity =>
        {
            entity.ToTable("document_storage_destinations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.FolderPath).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.SiteIdentifier).HasMaxLength(250);
            entity.Property(item => item.DriveIdentifier).HasMaxLength(250);
            entity.Property(item => item.FolderIdentifier).HasMaxLength(250);
            entity.Property(item => item.DisplayUrl).HasMaxLength(1000);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.StorageConnectionId, item.DocumentType, item.OperatingUnitId }).IsUnique();
            entity.HasIndex(item => new { item.OrganizationId, item.DocumentType, item.Status, item.ExternalTransferEnabled });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.StorageConnection).WithMany(item => item.Destinations).HasForeignKey(item => item.StorageConnectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserDocumentStorageAssignment>(entity =>
        {
            entity.ToTable("user_document_storage_assignments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.SiteIdentifier).HasMaxLength(250);
            entity.Property(item => item.DriveIdentifier).HasMaxLength(250);
            entity.Property(item => item.FolderIdentifier).HasMaxLength(250);
            entity.Property(item => item.DestinationUrl).HasMaxLength(1000);
            entity.HasIndex(item => item.DocumentStorageDestinationId);
            entity.HasIndex(item => item.UserId).IsUnique();
            entity.HasOne(item => item.User).WithMany(user => user.DocumentStorageAssignments).HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.StorageConnection).WithMany(connection => connection.Assignments).HasForeignKey(item => item.StorageConnectionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.DocumentStorageDestination).WithMany(destination => destination.UserAssignments).HasForeignKey(item => item.DocumentStorageDestinationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SupplierCode).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(250).IsRequired();
            entity.Property(item => item.NormalizedName).HasMaxLength(250).IsRequired();
             entity.Property(item => item.SearchName).HasMaxLength(250);
             entity.Property(item => item.BusinessPartnerId).HasMaxLength(100);
            entity.Property(item => item.TaxNumber).HasMaxLength(100);
             entity.Property(item => item.Trn).HasMaxLength(100);
            entity.Property(item => item.Email).HasMaxLength(250);
            entity.Property(item => item.Phone).HasMaxLength(100);
            entity.Property(item => item.EntityCode).HasMaxLength(100).HasDefaultValue("DEFAULT").IsRequired();
            entity.Property(item => item.LegalName).HasMaxLength(250);
            entity.Property(item => item.Address).HasMaxLength(1000);
            entity.Property(item => item.Country).HasMaxLength(100);
             entity.Property(item => item.City).HasMaxLength(150);
             entity.Property(item => item.PostalCode).HasMaxLength(50);
             entity.Property(item => item.Street).HasMaxLength(250);
            entity.Property(item => item.CompanyCode).HasMaxLength(100);
            entity.Property(item => item.PurchasingOrganization).HasMaxLength(100);
            entity.Property(item => item.Currency).HasMaxLength(10);
            entity.Property(item => item.PaymentTerms).HasMaxLength(100);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
             entity.Property(item => item.SourceSystem).HasMaxLength(100);
             entity.Property(item => item.SourceLastChangedAt);
             entity.Property(item => item.IsActive).HasDefaultValue(true);
            entity.HasIndex(item => new { item.OrganizationId, item.EntityCode, item.SupplierCode }).IsUnique();
            entity.HasIndex(item => new { item.OrganizationId, item.NormalizedName });
            entity.HasIndex(item => new { item.OrganizationId, item.SearchName });
            entity.HasIndex(item => new { item.OrganizationId, item.Trn });
             entity.HasIndex(item => new { item.OrganizationId, item.EntityCode, item.TaxNumber });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierAlias>(entity =>
        {
            entity.ToTable("supplier_aliases");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Alias).HasMaxLength(250).IsRequired();
            entity.Property(item => item.NormalizedAlias).HasMaxLength(250).IsRequired();
            entity.Property(item => item.SourceSystem).HasMaxLength(100);
             entity.Property(item => item.EntityCode).HasMaxLength(100).HasDefaultValue("DEFAULT").IsRequired();
             entity.Property(item => item.Confidence).HasPrecision(5, 4);
             entity.HasIndex(item => new { item.OrganizationId, item.EntityCode, item.NormalizedAlias }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Supplier).WithMany(item => item.Aliases).HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.ToTable("materials");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MaterialCode).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500).IsRequired();
            entity.Property(item => item.NormalizedDescription).HasMaxLength(500).IsRequired();
            entity.Property(item => item.BaseUom).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Category).HasMaxLength(150);
            entity.Property(item => item.MaterialGroup).HasMaxLength(150);
            entity.Property(item => item.MaterialType).HasMaxLength(40);
            entity.Property(item => item.AlternateUom).HasMaxLength(50);
            entity.Property(item => item.ConvFactor).HasPrecision(18, 6);
            entity.Property(item => item.ConvUnit).HasMaxLength(40);
            entity.Property(item => item.ConvValue).HasPrecision(18, 6);
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.CompanyCode).HasMaxLength(40);
            entity.Property(item => item.ValuationArea).HasMaxLength(40);
            entity.Property(item => item.ValuationClass).HasMaxLength(40);
            entity.Property(item => item.PriceControl).HasMaxLength(8);
            entity.Property(item => item.StandardPrice).HasPrecision(18, 4);
            entity.Property(item => item.MovingAveragePrice).HasPrecision(18, 4);
            entity.Property(item => item.AcquisitionSource).HasConversion<string>().HasMaxLength(16);
            entity.Property(item => item.GovernanceStatus).HasConversion<string>().HasMaxLength(24);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.InventoryType).HasConversion<string>().HasMaxLength(16).HasDefaultValue(InventoryItemType.NON_STOCK);
            entity.HasIndex(item => new { item.OrganizationId, item.MaterialCode }).IsUnique();
            entity.HasIndex(item => new { item.OrganizationId, item.NormalizedDescription });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SupplierMaterial>(entity =>
        {
            entity.ToTable("supplier_materials");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SupplierMaterialCode).HasMaxLength(150);
            entity.Property(item => item.SupplierDescription).HasMaxLength(500);
            entity.Property(item => item.PurchaseUom).HasMaxLength(50);
            entity.HasIndex(item => new { item.SupplierId, item.SupplierMaterialCode });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Supplier).WithMany(item => item.Materials).HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.OriginalFilename).HasMaxLength(500).IsRequired();
            entity.Property(item => item.ContentType).HasMaxLength(150).IsRequired();
            entity.Property(item => item.StorageProvider).HasMaxLength(50).IsRequired();
            entity.Property(item => item.StorageReference).IsRequired();
            entity.Property(item => item.ScanSessionId).HasMaxLength(100);
            entity.Property(item => item.OcrRequestId).HasMaxLength(100);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.SourceChannel).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.Status });
            entity.HasIndex(item => new { item.OrganizationId, item.ScanSessionId }).IsUnique().HasFilter("\"ScanSessionId\" IS NOT NULL");
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.UploadedByUser).WithMany().HasForeignKey(item => item.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentTransferJob>(entity =>
        {
            entity.ToTable("document_transfer_jobs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DocumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.Provider).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.ResolutionSource).HasMaxLength(30);
            entity.Property(item => item.ExternalFileId).HasMaxLength(500);
            entity.Property(item => item.ExternalWebUrl).HasMaxLength(1000);
            entity.Property(item => item.ExternalFileName).HasMaxLength(500);
            entity.Property(item => item.LastErrorCode).HasMaxLength(100);
            entity.Property(item => item.LastErrorMessageSafe).HasMaxLength(1000);
            entity.HasIndex(item => new { item.DocumentId, item.DestinationId }).IsUnique();
            entity.HasIndex(item => new { item.Status, item.NextAttemptAt });
            entity.HasOne(item => item.Document).WithMany(document => document.TransferJobs).HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Destination).WithMany(destination => destination.TransferJobs).HasForeignKey(item => item.DestinationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentPage>(entity =>
        {
            entity.ToTable("document_pages");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.DocumentId, item.PageNumber }).IsUnique();
            entity.HasOne(item => item.Document).WithMany(item => item.Pages).HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentExtraction>(entity =>
        {
            entity.ToTable("document_extractions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ExtractionType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.Provider).HasMaxLength(100).IsRequired();
            entity.Property(item => item.ProviderReference).HasMaxLength(500);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.ExtractionMethod).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.Trigger).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.OcrRequestId).HasMaxLength(100);
            entity.Property(item => item.ContentHash).HasMaxLength(128);
            entity.Property(item => item.ConfigurationSnapshotJson).HasColumnType("text");
            entity.Property(item => item.StructuredPayloadJson).HasColumnType("text");
            entity.HasIndex(item => new { item.DocumentId, item.ExtractionType });
            entity.HasOne(item => item.Document).WithMany(item => item.Extractions).HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(item => item.ContentHash).HasMaxLength(64);
            entity.HasIndex(item => new { item.OrganizationId, item.ContentHash });
        });

        modelBuilder.Entity<InvoiceExtData>(entity =>
        {
            entity.ToTable("invoice_ext_data");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SupplierName).HasMaxLength(500);
            entity.Property(item => item.SupplierTrn).HasMaxLength(150);
            entity.Property(item => item.SupplierInvoiceNumber).HasMaxLength(150);
            entity.Property(item => item.PurchaseOrderNumber).HasMaxLength(150);
            entity.Property(item => item.Currency).HasMaxLength(10);
            entity.Property(item => item.ItemSkuId).HasMaxLength(200);
            entity.Property(item => item.ItemDescription).HasMaxLength(1500);
            entity.Property(item => item.LineItemNumber).HasMaxLength(100);
            entity.Property(item => item.ItemAmount).HasPrecision(18, 4);
            entity.Property(item => item.ItemNet).HasPrecision(18, 4);
            entity.Property(item => item.InvoiceGross).HasPrecision(18, 4);
            entity.Property(item => item.InvoiceNet).HasPrecision(18, 4);
            entity.Property(item => item.SourceProvider).HasMaxLength(100).IsRequired();
            entity.Property(item => item.ExtractionMethod).HasMaxLength(50).IsRequired();
            entity.Property(item => item.ExtractionConfidence).HasPrecision(10, 4);
            entity.HasIndex(item => item.DocumentId);
            entity.HasIndex(item => item.OrganizationId);
            entity.HasIndex(item => item.SupplierInvoiceNumber);
            entity.HasIndex(item => item.PurchaseOrderNumber);
            entity.HasIndex(item => item.SupplierTrn);
            entity.HasIndex(item => item.ItemSkuId);
            entity.HasIndex(item => item.PurchaseOrderItemId);
            entity.HasOne(item => item.Document).WithMany().HasForeignKey(item => item.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PurchaseOrderItem).WithMany().HasForeignKey(item => item.PurchaseOrderItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExtractionAgentConfig>(entity =>
        {
            entity.ToTable("extraction_agent_configs");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.DocumentType).HasMaxLength(50).IsRequired();
            entity.Property(item => item.ProviderType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.EndpointUrl);
            entity.Property(item => item.AuthenticationType).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.CredentialReference).HasMaxLength(500);
            entity.Property(item => item.CredentialLast4).HasMaxLength(8);
            entity.Property(item => item.ConfigurationJson);
            entity.HasIndex(item => new { item.OrganizationId, item.DocumentType, item.IsActive, item.Priority });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceOcrConfiguration>(entity =>
        {
            entity.ToTable("invoice_ocr_configurations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.OrganizationId).IsUnique();
            entity.Property(item => item.BackendProvider).HasMaxLength(100).IsRequired();
            entity.Property(item => item.MinimumMobileConfidence).HasPrecision(5, 4);
            entity.Property(item => item.AmountTolerance).HasPrecision(18, 4);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.UpdatedByUser).WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.InvoiceNumber).HasMaxLength(150).IsRequired();
            entity.Property(item => item.SupplierNameRaw).HasMaxLength(500);
            entity.Property(item => item.SupplierTaxNumberRaw).HasMaxLength(150);
            entity.Property(item => item.PoNumberRaw).HasMaxLength(150);
            entity.Property(item => item.Currency).HasMaxLength(10);
            entity.Property(item => item.InvoiceType).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.NetAmount).HasPrecision(18, 4);
            entity.Property(item => item.TaxAmount).HasPrecision(18, 4);
            entity.Property(item => item.GrossAmount).HasPrecision(18, 4);
            entity.Property(item => item.SupplierLegalName).HasMaxLength(500);
            entity.Property(item => item.SupplierAddress).HasMaxLength(1000);
            entity.Property(item => item.SupplierEmail).HasMaxLength(320);
            entity.Property(item => item.SupplierPhone).HasMaxLength(100);
            entity.Property(item => item.DiscountAmount).HasPrecision(18, 4);
            entity.Property(item => item.FreightAmount).HasPrecision(18, 4);
            entity.Property(item => item.OtherCharges).HasPrecision(18, 4);
            entity.Property(item => item.TaxableAmount).HasPrecision(18, 4);
            entity.Property(item => item.AmountDue).HasPrecision(18, 4);
            entity.Property(item => item.PaymentTerms).HasMaxLength(500);
            entity.Property(item => item.ExtractionStatus).HasMaxLength(40);
            entity.Property(item => item.ExtractionProvider).HasMaxLength(100);
            entity.Property(item => item.ManualEditedFieldsJson).HasColumnType("text");
            entity.Property(item => item.OverallConfidence).HasPrecision(5, 4);
            entity.HasIndex(item => new { item.OrganizationId, item.InvoiceNumber });
            entity.HasIndex(item => new { item.OrganizationId, item.SupplierId });
            entity.HasIndex(item => new { item.OrganizationId, item.Status });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Document).WithOne(item => item.Invoice).HasForeignKey<Invoice>(item => item.DocumentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Supplier).WithMany(item => item.Invoices).HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PurchaseOrder).WithMany(item => item.Invoices).HasForeignKey(item => item.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.ToTable("invoice_lines");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DescriptionRaw).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.SupplierMaterialCode).HasMaxLength(150);
            entity.Property(item => item.MaterialCodeRaw).HasMaxLength(150);
            entity.Property(item => item.Uom).HasMaxLength(50);
            entity.Property(item => item.Quantity).HasPrecision(18, 4);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 4);
            entity.Property(item => item.TaxRate).HasPrecision(10, 4);
            entity.Property(item => item.TaxAmount).HasPrecision(18, 4);
            entity.Property(item => item.LineAmount).HasPrecision(18, 4);
            entity.Property(item => item.DiscountAmount).HasPrecision(18, 4);
            entity.Property(item => item.GrossAmount).HasPrecision(18, 4);
            entity.Property(item => item.PoItemNumber).HasMaxLength(100);
            entity.Property(item => item.BatchNumber).HasMaxLength(150);
            entity.Property(item => item.Confidence).HasPrecision(5, 4);
            entity.Property(item => item.MatchStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.HasIndex(item => new { item.InvoiceId, item.LineNumber }).IsUnique();
            entity.HasOne(item => item.Invoice).WithMany(item => item.Lines).HasForeignKey(item => item.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PurchaseOrderItem).WithMany(item => item.InvoiceLines).HasForeignKey(item => item.PurchaseOrderItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PoNumber).HasMaxLength(150).IsRequired();
             entity.Property(item => item.PurchaseOrderType).HasMaxLength(50);
             entity.Property(item => item.CompanyCode).HasMaxLength(50);
             entity.Property(item => item.ErpSupplierId).HasMaxLength(100);
             entity.Property(item => item.SupplierName).HasMaxLength(250);
             entity.Property(item => item.PurchasingOrganization).HasMaxLength(100);
             entity.Property(item => item.PurchasingGroup).HasMaxLength(100);
             entity.Property(item => item.PaymentTerms).HasMaxLength(100);
             entity.Property(item => item.PoCategory).HasMaxLength(50);
            entity.Property(item => item.Currency).HasMaxLength(10).IsRequired();
             entity.Property(item => item.TotalNetAmount).HasPrecision(18, 4);
             entity.Property(item => item.TotalTaxAmount).HasPrecision(18, 4);
             entity.Property(item => item.TotalAmount).HasPrecision(18, 4);
             entity.Property(item => item.TotalOrderedQuantity).HasPrecision(18, 4);
             entity.Property(item => item.TotalReceivedQuantity).HasPrecision(18, 4);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.SourceSystem).HasMaxLength(50).IsRequired();
            entity.Property(item => item.EntityCode).HasMaxLength(100).HasDefaultValue("DEFAULT").IsRequired();
            entity.Property(item => item.ExternalId).HasMaxLength(250);
            entity.Property(item => item.SourceHash).HasMaxLength(128);
            entity.Property(item => item.SourceLastChangedAtRaw).HasMaxLength(100);
            entity.HasIndex(item => new { item.OrganizationId, item.EntityCode, item.PoNumber }).IsUnique();
            entity.HasIndex(item => new { item.OrganizationId, item.SourceConfigurationId, item.ExternalId }).IsUnique().HasFilter("\"SourceConfigurationId\" IS NOT NULL AND \"ExternalId\" IS NOT NULL");
            entity.HasIndex(item => new { item.OrganizationId, item.OperatingUnitId, item.Status });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Supplier).WithMany(item => item.PurchaseOrders).HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.ToTable("purchase_order_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MaterialCode).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.Uom).HasMaxLength(50).IsRequired();
             entity.Property(item => item.ItemNumber).HasMaxLength(50);
            entity.Property(item => item.OrderedQuantity).HasPrecision(18, 4);
            entity.Property(item => item.ReceivedQuantity).HasPrecision(18, 4);
            entity.Property(item => item.OpenQuantity).HasPrecision(18, 4);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 4);
             entity.Property(item => item.PriceQuantity).HasPrecision(18, 4);
             entity.Property(item => item.ItemAmount).HasPrecision(18, 4);
             entity.Property(item => item.TaxCode).HasMaxLength(30);
             entity.Property(item => item.TaxAmount).HasPrecision(18, 4);
             entity.Property(item => item.GrossItemAmount).HasPrecision(18, 4);
             entity.Property(item => item.Currency).HasMaxLength(10);
             entity.Property(item => item.MaterialGroup).HasMaxLength(100);
             entity.Property(item => item.Plant).HasMaxLength(100);
             entity.Property(item => item.StorageLocation).HasMaxLength(100);
             entity.Property(item => item.ItemCategory).HasMaxLength(50);
             entity.Property(item => item.AccountAssignmentCategory).HasMaxLength(50);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.ExternalId).HasMaxLength(250);
            entity.Property(item => item.SourceHash).HasMaxLength(128);
            entity.HasIndex(item => new { item.PurchaseOrderId, item.LineNumber }).IsUnique();
            entity.HasOne(item => item.PurchaseOrder).WithMany(item => item.Items).HasForeignKey(item => item.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.Property(item => item.BusinessStatus).HasMaxLength(40);
            entity.Property(item => item.ErpPostingStatus).HasMaxLength(40);
            entity.Property(item => item.ErpMaterialDocument).HasMaxLength(100);
            entity.Property(item => item.ErpDocumentYear).HasMaxLength(10);
            entity.Property(item => item.ErpResponseJson).HasColumnType("text");
            entity.Property(item => item.FailureCode).HasMaxLength(100);
            entity.Property(item => item.FailureMessage).HasMaxLength(2000);
        });

        modelBuilder.Entity<ApiIntegrationConfiguration>(entity =>
        {
            entity.ToTable("api_integration_configurations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EntityCode).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(2000);
            entity.Property(item => item.SystemKind).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.BaseUrl).HasMaxLength(2000).IsRequired();
            entity.Property(item => item.ResourcePath).HasMaxLength(1000);
            entity.Property(item => item.ServicePath).HasMaxLength(1000);
            entity.Property(item => item.EntitySet).HasMaxLength(250);
            entity.Property(item => item.HttpMethod).HasMaxLength(10).IsRequired();
            entity.Property(item => item.EnvironmentCode).HasMaxLength(20);
            entity.Property(item => item.CompanyCode).HasMaxLength(50);
            entity.Property(item => item.Plant).HasMaxLength(50);
            entity.Property(item => item.PropertyCode).HasMaxLength(100);
            entity.Property(item => item.DesignerJson).HasColumnType("text");
            entity.Property(item => item.ValidationFingerprint).HasMaxLength(128);
            entity.Property(item => item.ValidatedBy).HasMaxLength(200);
            entity.Property(item => item.ValidationStatus).HasMaxLength(30);
            entity.Property(item => item.ConnectionStatus).HasMaxLength(30);
            entity.Property(item => item.Protocol).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.ProcessType).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.AuthenticationType).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.ProtectedPassword).HasColumnType("text");
            entity.Property(item => item.ProtectedClientSecret).HasColumnType("text");
            entity.Property(item => item.ProtectedBearerToken).HasColumnType("text");
            entity.Property(item => item.TokenHeadersJson).HasColumnType("text");
            entity.Property(item => item.TokenBodyJson).HasColumnType("text");
            entity.HasIndex(item => new { item.OrganizationId, item.EntityCode, item.ProcessType });
            entity.HasIndex(item => new { item.OrganizationId, item.ProcessType, item.Status, item.Priority, item.EntityCode, item.CompanyCode, item.Plant });
            entity.HasIndex(item => new { item.OrganizationId, item.Status, item.NextRunAt });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OrganizationUnit).WithMany().HasForeignKey(item => item.OrganizationUnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntegrationSchemaSnapshot>(entity =>
        {
            entity.ToTable("integration_schema_snapshots");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MetadataUrl).HasMaxLength(2000).IsRequired();
            entity.Property(item => item.SchemaJson).HasColumnType("text").IsRequired();
            entity.HasIndex(item => new { item.ConfigurationId, item.DiscoveredAt });
            entity.HasOne(item => item.Configuration).WithMany(item => item.SchemaSnapshots).HasForeignKey(item => item.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiFieldMapping>(entity =>
        {
            entity.ToTable("api_field_mappings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceField).HasMaxLength(250).IsRequired();
            entity.Property(item => item.TargetField).HasMaxLength(250).IsRequired();
            entity.Property(item => item.SourceKind).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.SourceStructure).HasMaxLength(80);
            entity.Property(item => item.Transformation).HasMaxLength(50);
            entity.Property(item => item.NullPolicy).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasIndex(item => new { item.ConfigurationId, item.SourceField, item.TargetField }).IsUnique();
            entity.HasOne(item => item.Configuration).WithMany(item => item.FieldMappings).HasForeignKey(item => item.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiIntegrationExecution>(entity =>
        {
            entity.ToTable("api_integration_executions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Trigger).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.ErrorCode).HasMaxLength(100);
            entity.Property(item => item.ErrorMessageSafe).HasMaxLength(2000);
            entity.Property(item => item.DetailJson).HasColumnType("text");
            entity.HasIndex(item => new { item.ConfigurationId, item.StartedAt });
            entity.HasOne(item => item.Configuration).WithMany(item => item.Executions).HasForeignKey(item => item.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationConnectionTest>(entity =>
        {
            entity.ToTable("integration_connection_tests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Fingerprint).HasMaxLength(128).IsRequired();
            entity.Property(item => item.SystemKind).HasMaxLength(30).IsRequired();
            entity.Property(item => item.ProcessType).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Protocol).HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
            entity.Property(item => item.ErrorCode).HasMaxLength(80);
            entity.Property(item => item.ErrorMessageSafe).HasMaxLength(2000);
            entity.Property(item => item.ChecksJson).HasColumnType("text");
            entity.HasIndex(item => new { item.OrganizationId, item.ExpiresAt });
            entity.HasIndex(item => item.Id);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.GrnNumber).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(item => item.PostingProvider).HasMaxLength(100).IsRequired();
            entity.Property(item => item.ExternalReference).HasMaxLength(200);
            entity.Property(item => item.AsnReference).HasMaxLength(150);
            entity.Property(item => item.ExternalSystem).HasMaxLength(40);
            entity.HasIndex(item => new { item.OrganizationId, item.GrnNumber }).IsUnique();
            entity.HasIndex(item => new { item.OrganizationId, item.Status });
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PurchaseOrder).WithMany(item => item.GoodsReceipts).HasForeignKey(item => item.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Invoice).WithMany(item => item.GoodsReceipts).HasForeignKey(item => item.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Supplier).WithMany(item => item.GoodsReceipts).HasForeignKey(item => item.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.ToTable("goods_receipt_lines");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MaterialCode).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(1000).IsRequired();
            entity.Property(item => item.Uom).HasMaxLength(50).IsRequired();
            entity.Property(item => item.OpenQuantityBefore).HasPrecision(18, 4);
            entity.Property(item => item.InvoiceQuantity).HasPrecision(18, 4);
            entity.Property(item => item.ReceivedQuantity).HasPrecision(18, 4);
            entity.Property(item => item.AcceptedQuantity).HasPrecision(18, 4);
            entity.Property(item => item.DamagedQuantity).HasPrecision(18, 4);
            entity.Property(item => item.RejectedQuantity).HasPrecision(18, 4);
            entity.HasOne(item => item.GoodsReceipt).WithMany(item => item.Lines).HasForeignKey(item => item.GoodsReceiptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.PurchaseOrderItem).WithMany(item => item.GoodsReceiptLines).HasForeignKey(item => item.PurchaseOrderItemId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("stock_balances");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MaterialCode).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(18, 4);
            entity.Property(item => item.Uom).HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.OperatingUnitId, item.MaterialCode, item.Uom }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("inventory_transactions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MaterialCode).HasMaxLength(150).IsRequired();
            entity.Property(item => item.TransactionType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(item => item.ReferenceType).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(18, 4);
            entity.Property(item => item.Uom).HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.OperatingUnitId, item.CreatedAt });
            entity.HasIndex(item => item.ReferenceId);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OperatingUnit).WithMany().HasForeignKey(item => item.OperatingUnitId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Operation).HasMaxLength(100).IsRequired();
            entity.Property(item => item.RequestHash).HasMaxLength(200);
            entity.Property(item => item.ResponseReference).HasMaxLength(200);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.UserId, item.IdempotencyKey, item.Operation }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Reference).HasMaxLength(200);
            entity.Property(item => item.OldStateJson);
            entity.Property(item => item.NewStateJson);
            entity.Property(item => item.Reason).HasMaxLength(500);
            entity.Property(item => item.Result).HasMaxLength(30);
            entity.HasIndex(item => new { item.OrganizationId, item.CreatedAt });
        });

        modelBuilder.Entity<MicrosoftAuthorizationState>(entity =>
        {
            entity.ToTable("microsoft_authorization_states");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StateHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ReturnUrl).HasMaxLength(200).IsRequired();
            entity.Property(item => item.DraftJson).IsRequired();
            entity.HasIndex(item => item.StateHash).IsUnique();
            entity.HasIndex(item => new { item.UserId, item.ExpiresAt });
            entity.HasOne(item => item.User).WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IntegrationRoute>(entity =>
        {
            entity.ToTable("integration_routes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProcessType).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.SystemKind).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.ProcessType, item.IsActive });
            entity.HasIndex(item => item.ApiIntegrationConfigurationId);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Configuration).WithMany(item => item.IntegrationRoutes).HasForeignKey(item => item.ApiIntegrationConfigurationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IntegrationRouteCompanyCode>(entity =>
        {
            entity.ToTable("integration_route_company_codes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CompanyCode).HasMaxLength(50).IsRequired();
            entity.HasIndex(item => new { item.IntegrationRouteId, item.CompanyCode }).IsUnique();
            entity.HasOne(item => item.IntegrationRoute).WithMany(item => item.CompanyCodes).HasForeignKey(item => item.IntegrationRouteId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyCodeMaster>(entity =>
        {
            entity.ToTable("company_code_masters");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CompanyCode).HasMaxLength(40).IsRequired();
            entity.Property(item => item.CompanyName).HasMaxLength(250).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.CompanyCode }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PropertyMaster>(entity =>
        {
            entity.ToTable("property_masters");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PropertyCode).HasMaxLength(40).IsRequired();
            entity.Property(item => item.PropertyName).HasMaxLength(250).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.PropertyCode }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        ConfigureRecipeManagement(modelBuilder);
        ConfigureApprovalWorkflows(modelBuilder);
        ConfigureInventoryFoundation(modelBuilder);
        ConfigureStockCount(modelBuilder);
    }
}