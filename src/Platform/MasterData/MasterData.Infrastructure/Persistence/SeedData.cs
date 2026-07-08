using Microsoft.Extensions.DependencyInjection;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using MasterData.Domain.Entities;

namespace MasterData.Infrastructure.Persistence
{
    /// <remarks>
    /// This method is excluded from code coverage analysis as it is used internally for seed the data to all the table for
    /// the Integration testing and does not contribute to the public API surface.
    /// </remarks>
    public static class SeedData
    {


        private static void CreateApiConfig(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<ApiConfig> entries = csvReader.GetRecords<ApiConfig>();
                List<ApiConfig> csvCount = entries.ToList();
                foreach (ApiConfig entry in csvCount)
                {
                    var existingEntry = context.ApiConfigs.Find(entry.Id);

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
                        _ = context.ApiConfigs.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        private static void CreateEmailContent(Stream stream, RepositoryContext context)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    IgnoreReferences = true,
                    BadDataFound = null
                };
                CsvReader csvReader = new CsvReader(reader, conf);
                System.Collections.Generic.IEnumerable<EmailContent> entries = csvReader.GetRecords<EmailContent>();
                List<EmailContent> csvCount = entries.ToList();
                foreach (EmailContent entry in csvCount)
                {
                    var existingEntry = context.EmailContents.Find(entry.Id);

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
                        _ = context.EmailContents.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        public static void CreateMetadata(Stream stream, RepositoryContext context)
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

                IEnumerable<Metadata> entries = csvReader.GetRecords<Metadata>();

                List<Metadata> csvCount = entries.ToList();

                foreach (Metadata entry in csvCount)
                {
                    var existingEntry = context.Metadata.Find(entry.Id);

                    if (existingEntry != null)
                    {
                        var originalValues = context.Entry(existingEntry).OriginalValues;

                        context.Entry(existingEntry)
                            .CurrentValues.SetValues(entry);

                        existingEntry.DateCreated =
                            originalValues.GetValue<DateTime>("DateCreated");

                        existingEntry.CreatedBy =
                            originalValues.GetValue<Guid>("CreatedBy");

                        existingEntry.IsActive =
                            originalValues.GetValue<bool>("IsActive");
                    }
                    else
                    {
                        _ = context.Metadata.Add(entry);
                    }
                }

                SaveEntities(context);

                csvReader.Dispose();
            }
        }
        public static void Initialize(IServiceProvider serviceProvider)
        {
            RepositoryContext context = serviceProvider.GetRequiredService<RepositoryContext>();
            context.Database.EnsureCreated();

            string basePath = Path.Combine(AppContext.BaseDirectory, "MasterData.Infrastructure", "Migrations");

            Stream stream = new FileStream(Path.Combine(basePath, "ApiConfig.csv"), FileMode.Open, FileAccess.Read);
            CreateApiConfig(stream, context);

            stream = new FileStream(Path.Combine(basePath, "EmailContent.csv"), FileMode.Open, FileAccess.Read);
            CreateEmailContent(stream, context);

            stream = new FileStream(Path.Combine(basePath, "Metadata.csv"), FileMode.Open, FileAccess.Read);
            CreateMetadata(stream, context);
        }

        public static void SaveEntities(RepositoryContext repositoryContext)
        {
            repositoryContext.OnBeforeSaving(Guid.Parse("DF78056A-1097-430C-B29A-0CC42E3ECE7B"));
            _ = repositoryContext.SaveChanges(true);
        }
    }
}