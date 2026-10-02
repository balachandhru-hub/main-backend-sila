using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Data;

public sealed partial class SilaMeDbContext
{
    public DbSet<InventoryLocation> InventoryLocations => Set<InventoryLocation>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryStockTransaction> InventoryStockTransactions => Set<InventoryStockTransaction>();
    public DbSet<InternalTransferOrder> InternalTransferOrders => Set<InternalTransferOrder>();
    public DbSet<InternalTransferLine> InternalTransferLines => Set<InternalTransferLine>();
    public DbSet<InternalTransferApproval> InternalTransferApprovals => Set<InternalTransferApproval>();
    public DbSet<InventoryWorkflowEvent> InventoryWorkflowEvents => Set<InventoryWorkflowEvent>();
    public DbSet<InventoryAlert> InventoryAlerts => Set<InventoryAlert>();
    public DbSet<PhysicalInventoryRequest> PhysicalInventoryRequests => Set<PhysicalInventoryRequest>();
    public DbSet<InternalPurchaseRequest> InternalPurchaseRequests => Set<InternalPurchaseRequest>();
    public DbSet<QuickTransferPolicy> QuickTransferPolicies => Set<QuickTransferPolicy>();
    public DbSet<UserInventoryLocation> UserInventoryLocations => Set<UserInventoryLocation>();
    public DbSet<MaterialLocation> MaterialLocations => Set<MaterialLocation>();

    private void ConfigureInventoryFoundation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryLocation>(entity =>
        {
            entity.ToTable("inventory_locations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.LocationCode).HasMaxLength(80).IsRequired();
            entity.Property(item => item.LocationName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.LocationType).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500);
            entity.Property(item => item.CompanyCode).HasMaxLength(40);
            entity.Property(item => item.GeneralLedgerNumber).HasMaxLength(40);
            entity.Property(item => item.CostCenter).HasMaxLength(40);
            entity.Property(item => item.ProfitCenter).HasMaxLength(40);
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.ManagerGroup).HasMaxLength(80);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.LocationCode }).IsUnique();
            entity.HasOne(item => item.ParentLocation).WithMany().HasForeignKey(item => item.ParentLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PropertyLocation).WithMany().HasForeignKey(item => item.PropertyLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.ToTable("inventory_balances");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OnHandQty).HasPrecision(18, 4);
            entity.Property(item => item.ReservedQty).HasPrecision(18, 4);
            entity.Property(item => item.AvailableQty).HasPrecision(18, 4);
            entity.Property(item => item.InTransitQty).HasPrecision(18, 4);
            entity.Property(item => item.InventoryValue).HasPrecision(18, 4);
            entity.Property(item => item.BaseUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.HasIndex(item => new { item.MaterialId, item.InventoryLocationId, item.BatchId }).IsUnique();
            entity.HasIndex(item => new { item.MaterialId, item.InventoryLocationId })
                .IsUnique()
                .HasFilter("\"BatchId\" IS NULL")
                .HasDatabaseName("IX_inventory_balances_material_location_nobatch");
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.InventoryLocation).WithMany().HasForeignKey(item => item.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InventoryStockTransaction>(entity =>
        {
            entity.ToTable("inventory_stock_transactions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.TransactionId).HasMaxLength(40).IsRequired();
            entity.Property(item => item.TransactionType).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Uom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.BaseUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Direction).HasConversion<string>().HasMaxLength(8).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(18, 4);
            entity.Property(item => item.BaseQuantity).HasPrecision(18, 4);
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Property(item => item.TransactionValue).HasPrecision(18, 4);
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.ReferenceType).HasMaxLength(40);
            entity.Property(item => item.Source).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(item => new { item.OrganizationId, item.TransactionId }).IsUnique();
            entity.HasIndex(item => new { item.MaterialId, item.InventoryLocationId, item.CreatedAt });
        });
        modelBuilder.Entity<InternalTransferOrder>(entity =>
        {
            entity.ToTable("internal_transfer_orders");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ItoNumber).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Mode).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.Reason).HasMaxLength(500);
            entity.Property(item => item.TotalValue).HasPrecision(18, 4);
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.HasIndex(item => new { item.OrganizationId, item.ItoNumber }).IsUnique();
            entity.HasOne(item => item.FromLocation).WithMany().HasForeignKey(item => item.FromInventoryLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.ToLocation).WithMany().HasForeignKey(item => item.ToInventoryLocationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InternalTransferLine>(entity =>
        {
            entity.ToTable("internal_transfer_lines");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Uom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.BaseUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.RequestedQty).HasPrecision(18, 4);
            entity.Property(item => item.ApprovedQty).HasPrecision(18, 4);
            entity.Property(item => item.DispatchedQty).HasPrecision(18, 4);
            entity.Property(item => item.ReceivedQty).HasPrecision(18, 4);
            entity.Property(item => item.BaseQty).HasPrecision(18, 4);
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Property(item => item.TransferValue).HasPrecision(18, 4);
            entity.Property(item => item.Comment).HasMaxLength(500);
            entity.HasOne(item => item.Ito).WithMany(item => item.Lines).HasForeignKey(item => item.ItoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<InternalTransferApproval>(entity =>
        {
            entity.ToTable("internal_transfer_approvals");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Side).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.ManagerGroup).HasMaxLength(80);
            entity.Property(item => item.Comment).HasMaxLength(500);
            entity.HasOne(item => item.Ito).WithMany(item => item.Approvals).HasForeignKey(item => item.ItoId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<InventoryWorkflowEvent>(entity =>
        {
            entity.ToTable("inventory_workflow_events");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ReferenceType).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Action).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Comment).HasMaxLength(1000);
        });
        modelBuilder.Entity<InventoryAlert>(entity =>
        {
            entity.ToTable("inventory_alerts");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(200);
            entity.Property(item => item.Message).HasMaxLength(1000);
            entity.Property(item => item.ReferenceType).HasMaxLength(40);
            entity.Property(item => item.RecommendedAction).HasConversion<string>().HasMaxLength(40);
        });
        modelBuilder.Entity<PhysicalInventoryRequest>(entity =>
        {
            entity.ToTable("physical_inventory_requests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Reason).HasMaxLength(500).IsRequired();
            entity.Property(item => item.Priority).HasMaxLength(16).IsRequired();
            entity.Property(item => item.AssignedGroup).HasMaxLength(80);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        });
        modelBuilder.Entity<InternalPurchaseRequest>(entity =>
        {
            entity.ToTable("internal_purchase_requests");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Uom).HasMaxLength(40);
            entity.Property(item => item.Reason).HasMaxLength(500);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(18, 4);
        });
        modelBuilder.Entity<QuickTransferPolicy>(entity =>
        {
            entity.ToTable("quick_transfer_policies");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.OrganizationId).IsUnique();
            entity.Property(item => item.AllowedSourceLocationTypes).HasMaxLength(120).IsRequired();
            entity.Property(item => item.AllowedDestinationLocationTypes).HasMaxLength(120).IsRequired();
            entity.Property(item => item.MaximumQuantity).HasPrecision(18, 4);
            entity.Property(item => item.MaximumValue).HasPrecision(18, 4);
        });
        modelBuilder.Entity<UserInventoryLocation>(entity =>
        {
            entity.ToTable("user_inventory_locations");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.UserId, item.InventoryLocationId }).IsUnique();
            entity.HasOne(item => item.InventoryLocation).WithMany().HasForeignKey(item => item.InventoryLocationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MaterialLocation>(entity =>
        {
            entity.ToTable("material_locations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StockingStatus).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.StockingType).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.ReplenishmentMethod).HasMaxLength(80);
            entity.Property(item => item.MinimumStock).HasPrecision(18, 4);
            entity.Property(item => item.MaximumStock).HasPrecision(18, 4);
            entity.Property(item => item.ReorderPoint).HasPrecision(18, 4);
            entity.Property(item => item.SafetyStock).HasPrecision(18, 4);
            entity.Property(item => item.ParLevel).HasPrecision(18, 4);
            entity.HasIndex(item => new { item.MaterialId, item.InventoryLocationId }).IsUnique();
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.InventoryLocation).WithMany().HasForeignKey(item => item.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PreferredSourceLocation).WithMany().HasForeignKey(item => item.PreferredSourceLocationId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
