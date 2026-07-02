using MasterData.Domain.Common;
using MasterData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
       

        _ = modelBuilder.Entity<UnspscCategory>()
            .HasIndex(x => new
            {
                x.IsActive,
                x.Segment,
                x.Family,
                x.Class,
                x.Commodity
            });

        base.OnModelCreating(modelBuilder);

       
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