using MasterData.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MasterData.Infrastructure.Persistence;

public class RepositoryContext : DbContext
{
    public RepositoryContext(DbContextOptions<RepositoryContext> options)
        : base(options)
    {
    }

    public DbSet<UnspscCategory> UnspscCategories { get; set; }
}