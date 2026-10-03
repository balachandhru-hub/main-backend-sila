using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Data;

public sealed partial class SilaMeDbContext
{
    public DbSet<MaterialBarcode> MaterialBarcodes => Set<MaterialBarcode>();
    public DbSet<StockCountSession> StockCountSessions => Set<StockCountSession>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<StockCountCapture> StockCountCaptures => Set<StockCountCapture>();
    public DbSet<StockShortageEnquiry> StockShortageEnquiries => Set<StockShortageEnquiry>();
    public DbSet<StockShortageMessage> StockShortageMessages => Set<StockShortageMessage>();

    private void ConfigureStockCount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaterialBarcode>(entity =>
        {
            entity.ToTable("material_barcodes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Barcode).HasMaxLength(80).IsRequired();
            entity.Property(item => item.BarcodeType).HasMaxLength(24).IsRequired();
            entity.Property(item => item.PackUom).HasMaxLength(40);
            entity.Property(item => item.PackQuantity).HasPrecision(18, 4);
            entity.HasIndex(item => new { item.OrganizationId, item.Barcode }).IsUnique();
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockCountSession>(entity =>
        {
            entity.ToTable("stock_count_sessions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.CountNumber).HasMaxLength(40).IsRequired();
            entity.Property(item => item.CountType).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(2000);
            entity.HasIndex(item => new { item.OrganizationId, item.CountNumber }).IsUnique();
            entity.HasOne(item => item.InventoryLocation).WithMany().HasForeignKey(item => item.InventoryLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.PropertyLocation).WithMany().HasForeignKey(item => item.PropertyId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockCountLine>(entity =>
        {
            entity.ToTable("stock_count_lines");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SystemUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.PhysicalUom).HasMaxLength(40);
            entity.Property(item => item.OpenUom).HasMaxLength(40);
            entity.Property(item => item.BaseUom).HasMaxLength(40);
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.SapMaterialDocument).HasMaxLength(80);
            entity.Property(item => item.SapError).HasMaxLength(2000);
            entity.Property(item => item.SystemQty).HasPrecision(18, 4);
            entity.Property(item => item.PhysicalQty).HasPrecision(18, 4);
            entity.Property(item => item.FullQty).HasPrecision(18, 4);
            entity.Property(item => item.OpenQty).HasPrecision(18, 4);
            entity.Property(item => item.ConvertedPhysicalQty).HasPrecision(18, 4);
            entity.Property(item => item.VarianceQty).HasPrecision(18, 4);
            entity.Property(item => item.VariancePercent).HasPrecision(18, 4);
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Property(item => item.VarianceValue).HasPrecision(18, 4);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(item => item.SapStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(item => item.CountMethod).HasConversion<string>().HasMaxLength(16);
            entity.HasIndex(item => new { item.StockCountSessionId, item.MaterialId }).IsUnique();
            entity.HasOne(item => item.Session).WithMany(item => item.Lines).HasForeignKey(item => item.StockCountSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StockCountCapture>(entity =>
        {
            entity.ToTable("stock_count_captures");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FullUom).HasMaxLength(40);
            entity.Property(item => item.OpenUom).HasMaxLength(40);
            entity.Property(item => item.BaseUom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Note).HasMaxLength(500);
            entity.Property(item => item.FullQty).HasPrecision(18, 4);
            entity.Property(item => item.OpenQty).HasPrecision(18, 4);
            entity.Property(item => item.ConvertedQty).HasPrecision(18, 4);
            entity.Property(item => item.Method).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasOne(item => item.Line).WithMany(item => item.Captures).HasForeignKey(item => item.StockCountLineId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StockShortageEnquiry>(entity =>
        {
            entity.ToTable("stock_shortage_enquiries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EnquiryNumber).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Uom).HasMaxLength(40).IsRequired();
            entity.Property(item => item.Currency).HasMaxLength(8);
            entity.Property(item => item.AssignedManagerGroup).HasMaxLength(80);
            entity.Property(item => item.SystemQty).HasPrecision(18, 4);
            entity.Property(item => item.PhysicalQty).HasPrecision(18, 4);
            entity.Property(item => item.ShortageQty).HasPrecision(18, 4);
            entity.Property(item => item.UnitCost).HasPrecision(18, 4);
            entity.Property(item => item.ShortageValue).HasPrecision(18, 4);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Category).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(item => new { item.OrganizationId, item.EnquiryNumber }).IsUnique();
            entity.HasIndex(item => item.StockCountLineId).IsUnique();
            entity.HasOne(item => item.Line).WithOne(item => item.Enquiry).HasForeignKey<StockShortageEnquiry>(item => item.StockCountLineId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<StockShortageMessage>(entity =>
        {
            entity.ToTable("stock_shortage_messages");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Kind).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Comments).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.AttachmentName).HasMaxLength(260);
            entity.Property(item => item.Category).HasConversion<string>().HasMaxLength(40);
            entity.HasOne(item => item.Enquiry).WithMany(item => item.Messages).HasForeignKey(item => item.EnquiryId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
