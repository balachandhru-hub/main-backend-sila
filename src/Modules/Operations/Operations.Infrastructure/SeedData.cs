using Microsoft.Extensions.DependencyInjection;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure
{
    /// <summary>
    /// Start-up seeding of the Operations database.
    /// The module needs no reference data: organizations, users, roles and features live in Identity,
    /// and the OCR policy of an organization falls back to its built-in defaults until it is saved.
    /// No demo organization and no administrator are created.
    /// </summary>
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            RepositoryContext context = serviceProvider.GetRequiredService<RepositoryContext>();
            SaveEntities(context);
        }

        public static void SaveEntities(RepositoryContext repositoryContext)
        {
            repositoryContext.OnBeforeSaving(Guid.Empty);
            _ = repositoryContext.SaveChanges(true);
        }
    }
}
