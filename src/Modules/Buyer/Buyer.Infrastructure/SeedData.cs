using Microsoft.Extensions.DependencyInjection;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using  Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure
{
    /// <remarks>
    /// This method is excluded from code coverage analysis as it is used internally for seed the data to all the table for
    /// the Integration testing and does not contribute to the public API surface.
    /// </remarks>
    public static class SeedData
    {

        public static void CreateDefaultVerificationTemplate(Stream stream, RepositoryContext context)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    IgnoreReferences = true
                };
                CsvReader csvReader = new CsvReader(reader, conf);
                System.Collections.Generic.IEnumerable<DefaultVerificationTemplate> entries = csvReader.GetRecords<DefaultVerificationTemplate>();
                List<DefaultVerificationTemplate> csvCount = entries.ToList();
                foreach (DefaultVerificationTemplate entry in csvCount)
                {
                    var existingEntry = context.DefaultVerificationTemplate.Find(entry.Id);

                    if (existingEntry != null)
                    {
                        // Backup original values
                        var originalValues = context.Entry(existingEntry).OriginalValues;

                        // Update existing entry
                        context.Entry(existingEntry).CurrentValues.SetValues(entry);

                        // Restore original values for specific fields
                        existingEntry.DateCreated = originalValues.GetValue<DateTime>("DateCreated");
                        existingEntry.CreatedBy = originalValues.GetValue<Guid>("CreatedBy");
                        existingEntry.IsActive = originalValues.GetValue<bool>("IsActive");

                    }
                    else
                    {
                        // Add new entry
                        _ = context.DefaultVerificationTemplate.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }
 public static void CreateDefaultVerificationTemplateQuestion(Stream stream, RepositoryContext context)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    IgnoreReferences = true
                };
                CsvReader csvReader = new CsvReader(reader, conf);
                System.Collections.Generic.IEnumerable<DefaultVerificationTemplateQuestion> entries = csvReader.GetRecords<DefaultVerificationTemplateQuestion>();
                List<DefaultVerificationTemplateQuestion> csvCount = entries.ToList();
                foreach (DefaultVerificationTemplateQuestion entry in csvCount)
{
    var existingEntry = context.DefaultVerificationTemplateQuestion.Find(entry.Id);

    if (existingEntry != null)
    {
        var originalValues = context.Entry(existingEntry).OriginalValues;

        context.Entry(existingEntry).CurrentValues.SetValues(entry);

        existingEntry.DateCreated = originalValues.GetValue<DateTime>("DateCreated");
        existingEntry.CreatedBy = originalValues.GetValue<Guid>("CreatedBy");
        existingEntry.IsActive = originalValues.GetValue<bool>("IsActive");
    }
    else
    {
        context.DefaultVerificationTemplateQuestion.Add(entry);
    }
}
            }
        }

       
        





        // Removed PlanConfigDefault helper class. Now using PlanConfig model directly for CSV mapping.

        public static void Initialize(IServiceProvider serviceProvider)
        {
            RepositoryContext context = serviceProvider.GetRequiredService<RepositoryContext>();
            context.Database.EnsureCreated();

            string basePath = Path.Combine(AppContext.BaseDirectory, "Buyer.Infrastructure", "Migrations");

            Stream stream = new FileStream(Path.Combine(basePath, "DefaultVerificationTemplate.csv"), FileMode.Open, FileAccess.Read);

            CreateDefaultVerificationTemplate(stream, context);
            stream = new FileStream(Path.Combine(basePath, "DefaultVerificationTemplateQuestion.csv"), FileMode.Open, FileAccess.Read);

            CreateDefaultVerificationTemplateQuestion(stream, context);
           
           

        }

        public static void SaveEntities(RepositoryContext repositoryContext)
        {

            repositoryContext.OnBeforeSaving(Guid.Parse("DF78056A-1097-430C-B29A-0CC42E3ECE7B"));
            _ = repositoryContext.SaveChanges(true);
        }
    }
}