namespace SharedKernel.Tenancy
{
    public static class TenancyConstants
    {
        /// <summary>
        /// Header carrying the host the browser opened. The frontend sends it; services forward it to each other.
        /// </summary>
        public const string TENANT_HOST_HEADER = "X-Tenant-Host";

        /// <summary>Token claim holding the code of the realm the token was issued for.</summary>
        public const string TENANT_CLAIM = "Tenant";

        /// <summary>Token claims set when a platform support user signs in to a customer realm.</summary>
        public const string SUPPORT_USER_CLAIM = "SupportUserId";
        public const string SUPPORT_SESSION_CLAIM = "SupportSessionId";

        public const string DEFAULT_CONNECTION_NAME = "DefaultConnection";

        public const string CONFIG_REGISTRY_CONNECTION = "Tenancy:RegistryConnection";
        public const string CONFIG_REGISTRY_DATABASE = "Tenancy:RegistryDatabase";
        public const string CONFIG_REGISTRY_SCHEMA = "Tenancy:RegistrySchema";
        public const string CONFIG_DEFAULT_NAME = "Tenancy:DefaultName";
        public const string CONFIG_DEFAULT_ENVIRONMENT = "Tenancy:DefaultEnvironment";

        public const string DEFAULT_REGISTRY_DATABASE = "IdentitySystemDB";
        public const string DEFAULT_REGISTRY_SCHEMA = "identitysystem";
    }
}
