using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Models;

namespace SilaMe.Api.Data;

public sealed partial class SilaMeDbContext
{
    public DbSet<ApprovalWorkflowConfiguration> ApprovalWorkflowConfigurations => Set<ApprovalWorkflowConfiguration>();
    public DbSet<ApprovalWorkflowLevel> ApprovalWorkflowLevels => Set<ApprovalWorkflowLevel>();
    public DbSet<ApprovalWorkflowCondition> ApprovalWorkflowConditions => Set<ApprovalWorkflowCondition>();

    private void ConfigureApprovalWorkflows(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalWorkflowConfiguration>(entity =>
        {
            entity.ToTable("approval_workflow_configurations");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(160).IsRequired();
            entity.Property(item => item.ApprovalType).HasConversion<string>().HasMaxLength(40).IsRequired();
            entity.Property(item => item.Action).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(item => item.ScopeKind).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.ScopeValue).HasMaxLength(80);
            entity.HasIndex(item => item.OrganizationId);
            entity.HasOne(item => item.Organization).WithMany().HasForeignKey(item => item.OrganizationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ApprovalWorkflowLevel>(entity =>
        {
            entity.ToTable("approval_workflow_levels");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.RoleKey).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Label).HasMaxLength(160).IsRequired();
            entity.HasIndex(item => new { item.ConfigurationId, item.Level }).IsUnique();
            entity.HasOne(item => item.Configuration).WithMany(item => item.Levels).HasForeignKey(item => item.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ApprovalWorkflowCondition>(entity =>
        {
            entity.ToTable("approval_workflow_conditions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FieldKey).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Operator).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(item => item.Value).HasPrecision(18, 4);
            entity.HasOne(item => item.Configuration).WithMany(item => item.Conditions).HasForeignKey(item => item.ConfigurationId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
