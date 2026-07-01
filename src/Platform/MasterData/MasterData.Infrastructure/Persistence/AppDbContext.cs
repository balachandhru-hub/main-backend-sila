using MasterData.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MasterData.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<UnspscCategory> UnspscCategories { get; set; }
}