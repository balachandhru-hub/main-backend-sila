using Microsoft.EntityFrameworkCore;
using Identity.Domain.Entities;
using SharedKernel.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using SharedKernel.Util;
using Identity.Domain.Common;

namespace Identity.Infrastructure.DbContext
{
    public class RepositoryContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IConfiguration _configuration;

        public RepositoryContext(DbContextOptions<RepositoryContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        

        public DbSet<Organization> Organizations { get; set; }
        public DbSet<User> User {get;set;}
        public DbSet<Person>Person {get;set;}
        public DbSet<Role> Role {get;set;}
        public DbSet<RoleFeatureMapping> RoleFeatureMapping {get;set;}
        public DbSet<UserRoleMapping> UserRoleMapping {get; set;}
        public DbSet<Feature> Feature {get;set;}
        public DbSet<RefreshToken> RefreshToken {get;set;}
        public DbSet<EmailVerification> EmailVerification {get;set;}
        public DbSet<ApiKey> ApiKey {get;set;}
        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            _ = modelBuilder.HasDefaultSchema(_configuration[Common.APPLICATION_SCHEMA]);
            _ = modelBuilder.Entity<Person>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Organization>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<User>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<UserRoleMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Role>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RoleFeatureMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RefreshToken>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Feature>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<EmailVerification>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ApiKey>().HasIndex(a => a.IsActive);

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