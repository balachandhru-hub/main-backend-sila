using Supplier.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Supplier.Infrastructure
{
    /// <summary>
    /// Class <c>DBMigration</c> static class is used for DB migrations
    /// </summary>
    public static class DBMigration
    {
        /// <summary>
        /// Method <c>UpdateDatabase</c> is used to migrate DB changes
        /// </summary>
        /// <param name="serviceProvider">To get the context</param>
        public static void UpdateDatabase(IServiceProvider serviceProvider)
        {
            RepositoryContext context = serviceProvider.GetRequiredService<RepositoryContext>();
            context.Database.Migrate();
            context.SaveChangesAsync(true);
        }
    }
}