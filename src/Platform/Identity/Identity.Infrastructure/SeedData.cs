using Microsoft.Extensions.DependencyInjection;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;


namespace Identity.Infrastructure
{
    /// <remarks>
    /// This method is excluded from code coverage analysis as it is used internally for seed the data to all the table for
    /// the Integration testing and does not contribute to the public API surface.
    /// </remarks>
    public static class SeedData
    {

        public static void CreateRole(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<Role> entries = csvReader.GetRecords<Role>();
                List<Role> csvCount = entries.ToList();
                foreach (Role entry in csvCount)
                {
                    var existingEntry = context.Role.Find(entry.Id);

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
                        _ = context.Role.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        public static void CreatePerson(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<Person> entries = csvReader.GetRecords<Person>();
                List<Person> csvCount = entries.ToList();
                foreach (Person entry in csvCount)
                {
                    var existingEntry = context.Person.Find(entry.Id);

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
                        _ = context.Person.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        public static void CreateUser(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<User> entries = csvReader.GetRecords<User>();
                List<User> csvCount = entries.ToList();
                foreach (User entry in csvCount)
                {
                    var existingEntry = context.User.Find(entry.Id);

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
                        _ = context.User.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        public static void CreateUserRoleMapping(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<UserRoleMapping> entries = csvReader.GetRecords<UserRoleMapping>();
                List<UserRoleMapping> csvCount = entries.ToList();
                foreach (UserRoleMapping entry in csvCount)
                {
                    var existingEntry = context.UserRoleMapping.Find(entry.Id);

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
                        _ = context.UserRoleMapping.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        public static void CreateRoleFeatureMapping(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<RoleFeatureMapping> entries = csvReader.GetRecords<RoleFeatureMapping>();
                List<RoleFeatureMapping> csvCount = entries.ToList();
                foreach (RoleFeatureMapping entry in csvCount)
                {
                    var existingEntry = context.RoleFeatureMapping.Find(entry.Id);

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
                        _ = context.RoleFeatureMapping.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }

        public static void CreateFeature(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<Feature> entries = csvReader.GetRecords<Feature>();
                List<Feature> csvCount = entries.ToList();
                foreach (Feature entry in csvCount)
                {
                    var existingEntry = context.Feature.Find(entry.Id);

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
                        _ = context.Feature.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }
          public static void CreateOrganization(Stream stream, RepositoryContext context)
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
                System.Collections.Generic.IEnumerable<Organization> entries = csvReader.GetRecords<Organization>();
                List<Organization> csvCount = entries.ToList();
                foreach (Organization entry in csvCount)
                {
                    var existingEntry = context.Organizations.Find(entry.Id);

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
                        _ = context.Organizations.Add(entry);
                    }
                }
                SaveEntities(context);
                csvReader.Dispose();
            }
        }




        // Removed PlanConfigDefault helper class. Now using PlanConfig model directly for CSV mapping.

        public static void Initialize(IServiceProvider serviceProvider)
        {
            RepositoryContext context = serviceProvider.GetRequiredService<RepositoryContext>();
            context.Database.EnsureCreated();

            string basePath = Path.Combine(AppContext.BaseDirectory, "Identity.Infrastructure", "Migrations");

            Stream stream = new FileStream(Path.Combine(basePath, "Role.csv"), FileMode.Open, FileAccess.Read);

            CreateRole(stream, context);
            stream = new FileStream(Path.Combine(basePath, "Organization.csv"), FileMode.Open, FileAccess.Read);

            CreateOrganization(stream, context);

            stream = new FileStream(Path.Combine(basePath, "Person.csv"), FileMode.Open, FileAccess.Read);

            CreatePerson(stream, context);

            stream = new FileStream(Path.Combine(basePath, "User.csv"), FileMode.Open, FileAccess.Read);

            CreateUser(stream, context);

            stream = new FileStream(Path.Combine(basePath, "UserRoleMapping.csv"), FileMode.Open, FileAccess.Read);

            CreateUserRoleMapping(stream, context);

            stream = new FileStream(Path.Combine(basePath, "RoleFeatureMapping.csv"), FileMode.Open, FileAccess.Read);

            CreateRoleFeatureMapping(stream, context);

            stream = new FileStream(Path.Combine(basePath, "Feature.csv"), FileMode.Open, FileAccess.Read);

            CreateFeature(stream, context);
           

        }

        public static void SaveEntities(RepositoryContext repositoryContext)
        {

            repositoryContext.OnBeforeSaving(Guid.Parse("DF78056A-1097-430C-B29A-0CC42E3ECE7B"));
            _ = repositoryContext.SaveChanges(true);
        }
    }
}