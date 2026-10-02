using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Data;

public sealed partial class SilaMeDbContext
{
    public DbSet<MaterialValuation> MaterialValuations => Set<MaterialValuation>();
    public DbSet<MaterialChangeRequest> MaterialChangeRequests => Set<MaterialChangeRequest>();
    public DbSet<MaterialApprovalAction> MaterialApprovalActions => Set<MaterialApprovalAction>();
    public DbSet<MaterialErpSyncState> MaterialErpSyncStates => Set<MaterialErpSyncState>();
    public DbSet<RecipeFamily> RecipeFamilies => Set<RecipeFamily>();
    public DbSet<RecipeCategory> RecipeCategories => Set<RecipeCategory>();
    public DbSet<RecipeLocation> RecipeLocations => Set<RecipeLocation>();
    public DbSet<RecipeLocationAssignment> RecipeLocationAssignments => Set<RecipeLocationAssignment>();
    public DbSet<UomMaster> UomMasters => Set<UomMaster>();
    public DbSet<MaterialUomConversion> MaterialUomConversions => Set<MaterialUomConversion>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeVersion> RecipeVersions => Set<RecipeVersion>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeApprovalWorkflow> RecipeApprovalWorkflows => Set<RecipeApprovalWorkflow>();
    public DbSet<RecipeApprovalLevel> RecipeApprovalLevels => Set<RecipeApprovalLevel>();
    public DbSet<RecipeApprovalAction> RecipeApprovalActions => Set<RecipeApprovalAction>();
    public DbSet<PosSource> PosSources => Set<PosSource>();
    public DbSet<PosOutletMapping> PosOutletMappings => Set<PosOutletMapping>();
    public DbSet<OutletMenuItem> OutletMenuItems => Set<OutletMenuItem>();
    public DbSet<PosItemRecipeMapping> PosItemRecipeMappings => Set<PosItemRecipeMapping>();
    public DbSet<RecipeConsumptionTransaction> RecipeConsumptionTransactions => Set<RecipeConsumptionTransaction>();
    public DbSet<RecipeConsumptionLine> RecipeConsumptionLines => Set<RecipeConsumptionLine>();
    public DbSet<RecipeTransactionEvent> RecipeTransactionEvents => Set<RecipeTransactionEvent>();
    public DbSet<RecipeInventoryPosting> RecipeInventoryPostings => Set<RecipeInventoryPosting>();
    public DbSet<PosSalesUploadBatch> PosSalesUploadBatches => Set<PosSalesUploadBatch>();
    public DbSet<PosSalesUploadLine> PosSalesUploadLines => Set<PosSalesUploadLine>();
    public DbSet<RecipeSapPosting> RecipeSapPostings => Set<RecipeSapPosting>();

    private void ConfigureRecipeManagement(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaterialValuation>(entity =>
        {
            entity.ToTable("material_valuations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ValuationArea).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Plant).HasMaxLength(40);
            entity.Property(item => item.PriceUom).HasMaxLength(40);
            entity.Property(item => item.Source).HasMaxLength(40);
            entity.Property(item => item.StandardPrice).HasPrecision(18, 4);
            entity.Property(item => item.MovingAveragePrice).HasPrecision(18, 4);
            entity.HasIndex(item => new { item.MaterialId, item.ValuationArea }).IsUnique().HasFilter("\"EffectiveTo\" IS NULL");
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MaterialChangeRequest>(entity =>
        {
            entity.ToTable("material_change_requests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MaterialCode).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Event).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Source).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(1000);
            entity.Property(item => item.PriceUom).HasMaxLength(40);
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.CurrentUnitPrice).HasPrecision(18, 4);
            entity.Property(item => item.ProposedUnitPrice).HasPrecision(18, 4);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<MaterialApprovalAction>(entity =>
        {
            entity.ToTable("material_approval_actions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Event).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasOne(item => item.ChangeRequest).WithMany(item => item.Actions).HasForeignKey(item => item.ChangeRequestId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MaterialErpSyncState>(entity =>
        {
            entity.ToTable("material_erp_sync_states");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CompanyCode).HasMaxLength(40).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.CompanyCode }).IsUnique();
        });
        modelBuilder.Entity<RecipeFamily>(entity =>
        {
            entity.ToTable("recipe_families");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.Code }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeCategory>(entity =>
        {
            entity.ToTable("recipe_categories");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.Code }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UomMaster>(entity =>
        {
            entity.ToTable("uom_masters");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Code).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Dimension).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.Code }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MaterialUomConversion>(entity =>
        {
            entity.ToTable("material_uom_conversions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FromUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.ToUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Numerator).HasPrecision(18, 6);
            entity.Property(item => item.Denominator).HasPrecision(18, 6);
            entity.Property(item => item.PackSize).HasPrecision(18, 6);
            entity.Property(item => item.PackUom).HasMaxLength(40);
            entity.HasIndex(item => new { item.MaterialId, item.FromUom, item.ToUom });
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.ToTable("recipes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RecipeCode).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(250).IsRequired();
            entity.Property(item => item.Family).HasMaxLength(120);
            entity.Property(item => item.ServingUom).HasMaxLength(40);
            entity.Property(item => item.ServingSize).HasPrecision(18, 6);
            entity.Property(item => item.TotalServingQty).HasPrecision(18, 6);
            entity.Property(item => item.TotalServingUom).HasMaxLength(40);
            entity.Property(item => item.ItemMode).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.YieldQty).HasPrecision(18, 6);
            entity.Property(item => item.YieldUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.PosCode).HasMaxLength(80);
            entity.Property(item => item.PosItem).HasMaxLength(250);
            entity.Property(item => item.PosItemMenuPrice).HasPrecision(18, 4);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.RecipeCode }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Category).WithMany().HasForeignKey(item => item.CategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(item => item.RecipeFamily).WithMany().HasForeignKey(item => item.FamilyId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<RecipeLocation>(entity =>
        {
            entity.ToTable("recipe_locations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.Kind, item.Code }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeLocationAssignment>(entity =>
        {
            entity.ToTable("recipe_location_assignments");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.RecipeId, item.LocationId }).IsUnique();
            entity.HasOne(item => item.Recipe).WithMany(item => item.LocationAssignments).HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Location).WithMany(item => item.Assignments).HasForeignKey(item => item.LocationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeVersion>(entity =>
        {
            entity.ToTable("recipe_versions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(250).IsRequired();
            entity.Property(item => item.YieldUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.YieldQuantity).HasPrecision(18, 6);
            entity.Property(item => item.PortionSize).HasPrecision(18, 6);
            entity.Property(item => item.SnapshotMenuPrice).HasPrecision(18, 4);
            entity.Property(item => item.SnapshotTotalRecipeCost).HasPrecision(18, 4);
            entity.Property(item => item.SnapshotCostPerServing).HasPrecision(18, 4);
            entity.Property(item => item.SnapshotCostPercent).HasPrecision(9, 4);
            entity.Property(item => item.SnapshotMarginAmount).HasPrecision(18, 4);
            entity.Property(item => item.SnapshotMarginPercent).HasPrecision(9, 4);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.RecipeId, item.VersionNumber }).IsUnique();
            entity.HasOne(item => item.Recipe).WithMany(item => item.Versions).HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.ToTable("recipe_ingredients");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Uom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(18, 6);
            entity.Property(item => item.WastagePercent).HasPrecision(9, 4);
            entity.Property(item => item.YieldPercent).HasPrecision(9, 4);
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Property(item => item.ErpMaterialId).HasMaxLength(100);
            entity.Property(item => item.MaterialGroup).HasMaxLength(80);
            entity.Property(item => item.IngredientCost).HasPrecision(18, 4);
            entity.Property(item => item.PercentageOfTotalCost).HasPrecision(9, 4);
            entity.Property(item => item.RecipeIngredientId).HasMaxLength(40);
            entity.Property(item => item.ConsumptionQuantity).HasPrecision(18, 6);
            entity.Property(item => item.ConsumptionUom).HasMaxLength(40);
            entity.HasOne(item => item.RecipeVersion).WithMany(item => item.Ingredients).HasForeignKey(item => item.RecipeVersionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RecipeApprovalWorkflow>(entity =>
        {
            entity.ToTable("recipe_approval_workflows");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Event).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.Event }).IsUnique();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeApprovalLevel>(entity =>
        {
            entity.ToTable("recipe_approval_levels");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RoleKey).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Label).HasMaxLength(160).IsRequired();
            entity.HasOne(item => item.Workflow).WithMany(item => item.Levels).HasForeignKey(item => item.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeApprovalAction>(entity =>
        {
            entity.ToTable("recipe_approval_actions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Event).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasOne(item => item.Recipe).WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.RecipeVersion).WithMany(item => item.ApprovalActions).HasForeignKey(item => item.RecipeVersionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PosSource>(entity =>
        {
            entity.ToTable("pos_sources");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.PosSystem).HasMaxLength(80).IsRequired();
            entity.Property(item => item.IntegrationKind).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.ApiIntegrationConfiguration).WithMany().HasForeignKey(item => item.ApiIntegrationConfigurationId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<PosOutletMapping>(entity =>
        {
            entity.ToTable("pos_outlet_mappings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PosOutletCode).HasMaxLength(80).IsRequired();
            entity.HasIndex(item => new { item.PosSourceId, item.PosOutletCode }).IsUnique();
            entity.HasOne(item => item.PosSource).WithMany().HasForeignKey(item => item.PosSourceId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<OutletMenuItem>(entity =>
        {
            entity.ToTable("outlet_menu_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PosCode).HasMaxLength(80);
            entity.Property(item => item.PosItem).HasMaxLength(250);
            entity.HasIndex(item => new { item.OutletMappingId, item.RecipeId }).IsUnique();
            entity.HasOne(item => item.Outlet).WithMany(item => item.MenuItems).HasForeignKey(item => item.OutletMappingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Recipe).WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PosItemRecipeMapping>(entity =>
        {
            entity.ToTable("pos_item_recipe_mappings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.PosItemCode).HasMaxLength(80).IsRequired();
            entity.HasIndex(item => new { item.PosSourceId, item.PosItemCode }).IsUnique();
            entity.HasOne(item => item.PosSource).WithMany().HasForeignKey(item => item.PosSourceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Recipe).WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RecipeConsumptionTransaction>(entity =>
        {
            entity.ToTable("recipe_consumption_transactions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceSystem).HasMaxLength(80).IsRequired();
            entity.Property(item => item.SourceTransactionId).HasMaxLength(120).IsRequired();
            entity.Property(item => item.QuantitySold).HasPrecision(18, 6);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.PosSourceId, item.BusinessDate, item.SourceTransactionId, item.SourceLineNumber }).IsUnique();
            entity.HasIndex(item => item.UploadBatchId);
        });
        modelBuilder.Entity<RecipeConsumptionLine>(entity =>
        {
            entity.ToTable("recipe_consumption_lines");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Uom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.ConsumedQuantity).HasPrecision(18, 6);
            entity.HasOne(item => item.Transaction).WithMany(item => item.Lines).HasForeignKey(item => item.TransactionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeTransactionEvent>(entity =>
        {
            entity.ToTable("recipe_transaction_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Step).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
            entity.HasOne(item => item.Transaction).WithMany(item => item.Events).HasForeignKey(item => item.TransactionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RecipeInventoryPosting>(entity =>
        {
            entity.ToTable("recipe_inventory_postings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(item => item.TransactionId).IsUnique();
            entity.HasOne(item => item.Transaction).WithMany().HasForeignKey(item => item.TransactionId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PosSalesUploadBatch>(entity =>
        {
            entity.ToTable("pos_sales_upload_batches");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.UploadedAt });
        });
        modelBuilder.Entity<PosSalesUploadLine>(entity =>
        {
            entity.ToTable("pos_sales_upload_lines");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Qty).HasPrecision(18, 6);
            entity.HasOne(item => item.Batch).WithMany(item => item.Lines).HasForeignKey(item => item.BatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(item => item.BatchId);
        });
        modelBuilder.Entity<RecipeSapPosting>(entity =>
        {
            entity.ToTable("recipe_sap_postings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Plant).HasMaxLength(40).IsRequired();
            entity.Property(item => item.StorageLocation).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.UploadBatchId, item.BusinessDate, item.Plant, item.StorageLocation });
        });
    }
}
