using MasterData.Domain.Common;
using MasterData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using SharedKernel.Util;

namespace MasterData.Infrastructure.Persistence;

public class RepositoryContext : DbContext
{
    private readonly IConfiguration _configuration;

    public RepositoryContext(
        DbContextOptions<RepositoryContext> options,
        IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }

    public DbSet<UnspscCategory> UnspscCategories { get; set; }
    public DbSet<ApiConfig> ApiConfigs { get; set; }
    public DbSet<EmailContent> EmailContents { get; set; }
    public DbSet<EmailSentDetail> EmailSentDetails { get; set; }
    public DbSet<EmailFailedDetail> EmailFailedDetails { get; set; }
    public DbSet<EmailCCList> EmailCCLists { get; set; }

    protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        _ = modelBuilder.HasDefaultSchema(_configuration[MasterData.Domain.Common.Common.APPLICATION_SCHEMA]);

        _ = modelBuilder.Entity<UnspscCategory>().HasIndex(x => new { x.IsActive, x.Segment, x.Family, x.Class, x.Commodity });
        _ = modelBuilder.Entity<ApiConfig>().HasIndex(x => new { x.IsActive });
        _ = modelBuilder.Entity<EmailContent>().HasIndex(x => new { x.IsActive });
        _ = modelBuilder.Entity<EmailSentDetail>().HasIndex(x => new { x.IsActive });
        _ = modelBuilder.Entity<EmailFailedDetail>().HasIndex(x => new { x.IsActive });
        _ = modelBuilder.Entity<EmailCCList>().HasIndex(x => new { x.IsActive });

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


    public void OnBeforeSaving(Guid userId)
    {
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries =
            ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity trackable)
            {
                DateTime now = DateTime.UtcNow;

                switch (entry.State)
                {
                    case EntityState.Added:
                        trackable.DateCreated = now;
                        trackable.CreatedBy = userId;
                        trackable.DateUpdated = now;
                        trackable.UpdatedBy = userId;
                        trackable.IsActive = true;
                        break;

                    case EntityState.Modified:
                        trackable.DateUpdated = now;
                        trackable.UpdatedBy = userId;
                        break;
                }
            }
        }
    }
}