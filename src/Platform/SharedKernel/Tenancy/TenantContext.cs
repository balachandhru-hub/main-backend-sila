using System.Data.Common;
using Microsoft.Extensions.Configuration;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// The realm of the current request. Outside a request (startup, migrations) it is the default realm.
    /// </summary>
    public interface ITenantContext
    {
        TenantInfo Tenant { get; }

        /// <summary>The connection string of this service's database for the current realm.</summary>
        string ConnectionString { get; }

        void Use(TenantInfo tenant);
    }

    public class TenantContext : ITenantContext
    {
        // SQL Server accepts either keyword for the database name.
        private static readonly string[] DatabaseKeys = { "Initial Catalog", "Database" };

        private readonly IConfiguration _configuration;

        public TenantContext(IConfiguration configuration, ITenantRegistry registry)
        {
            _configuration = configuration;
            Tenant = registry.Default;
        }

        public TenantInfo Tenant { get; private set; }

        public string ConnectionString => ConnectionStringFor(_configuration, Tenant);

        public void Use(TenantInfo tenant)
        {
            Tenant = tenant;
        }

        /// <summary>
        /// The default realm uses the service's DefaultConnection as configured. Any other realm uses the
        /// same server (or the realm's own server key) with the database name suffixed by the realm code,
        /// for example BuyerSystemDB_acme.
        /// </summary>
        public static string ConnectionStringFor(IConfiguration configuration, TenantInfo tenant)
        {
            string defaultConnection = configuration.GetConnectionString(TenancyConstants.DEFAULT_CONNECTION_NAME)
                ?? throw new InvalidOperationException("'ConnectionStrings:DefaultConnection' is not configured.");
            if (tenant.IsDefault)
            {
                return defaultConnection;
            }

            string baseConnection = defaultConnection;
            if (!string.IsNullOrWhiteSpace(tenant.DatabaseServerKey))
            {
                baseConnection = configuration.GetConnectionString(tenant.DatabaseServerKey)
                    ?? throw new InvalidOperationException(
                        $"'ConnectionStrings:{tenant.DatabaseServerKey}' is not configured for realm '{tenant.Code}'.");
            }

            string databaseName = DatabaseName(defaultConnection)
                ?? throw new InvalidOperationException("'ConnectionStrings:DefaultConnection' does not name a database.");

            DbConnectionStringBuilder builder = new DbConnectionStringBuilder { ConnectionString = baseConnection };
            foreach (string key in DatabaseKeys)
            {
                builder.Remove(key);
            }

            builder["Initial Catalog"] = $"{databaseName}_{tenant.Code}";
            return builder.ConnectionString;
        }

        private static string? DatabaseName(string connectionString)
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            foreach (string key in DatabaseKeys)
            {
                if (builder.TryGetValue(key, out object? value) && value != null)
                {
                    return value.ToString();
                }
            }

            return null;
        }
    }
}
