namespace SharedKernel.Tenancy
{
    /// <summary>
    /// A customer realm: one customer environment with its own URL and its own databases.
    /// The default realm is the deployment's own configuration and is not stored in the registry.
    /// </summary>
    public class TenantInfo
    {
        public const string DEFAULT_CODE = "default";
        public const string ENVIRONMENT_PROD = "PROD";
        public const string ENVIRONMENT_TEST = "TEST";

        public Guid Id { get; set; }

        /// <summary>
        /// Short lowercase identifier. It is the suffix of the realm's database names.
        /// </summary>
        public string Code { get; set; } = DEFAULT_CODE;

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Groups the environments (PROD, TEST) of one customer.
        /// </summary>
        public string CustomerCode { get; set; } = string.Empty;

        public string Environment { get; set; } = ENVIRONMENT_PROD;

        /// <summary>
        /// Host the customer opens in the browser, with the port when it is not the default one.
        /// </summary>
        public string HostName { get; set; } = string.Empty;

        /// <summary>
        /// Name of a connection string in each service's configuration to use as the database server
        /// for this realm. Empty uses the service's DefaultConnection server.
        /// </summary>
        public string? DatabaseServerKey { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDefault { get; set; }

        /// <summary>
        /// Licensed modules that are valid today. The default realm is not licence-restricted.
        /// </summary>
        public HashSet<string> Modules { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool HasModule(string moduleKey)
        {
            return IsDefault || Modules.Contains(moduleKey);
        }
    }
}
