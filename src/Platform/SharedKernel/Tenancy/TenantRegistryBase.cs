using Microsoft.Extensions.Configuration;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// In-memory realm lookup. A subclass says where the realms are loaded from.
    /// </summary>
    public abstract class TenantRegistryBase : ITenantRegistry
    {
        // Replaced as a whole on refresh, so readers never see a half-built list.
        private volatile IReadOnlyList<TenantInfo> _tenants = Array.Empty<TenantInfo>();

        protected TenantRegistryBase(IConfiguration configuration)
        {
            Default = new TenantInfo
            {
                Id = Guid.Empty,
                Code = TenantInfo.DEFAULT_CODE,
                Name = configuration[TenancyConstants.CONFIG_DEFAULT_NAME] ?? "Default",
                CustomerCode = TenantInfo.DEFAULT_CODE,
                Environment = configuration[TenancyConstants.CONFIG_DEFAULT_ENVIRONMENT] ?? TenantInfo.ENVIRONMENT_PROD,
                IsDefault = true,
                IsActive = true
            };
        }

        public TenantInfo Default { get; }

        public TenantInfo? FindByHost(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return null;
            }

            string wanted = host.Trim().TrimEnd('/');
            IReadOnlyList<TenantInfo> tenants = _tenants;
            TenantInfo? exact = tenants.FirstOrDefault(
                tenant => string.Equals(tenant.HostName, wanted, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            // A realm registered without a port matches the same host on any port.
            string withoutPort = wanted.Split(':')[0];
            return tenants.FirstOrDefault(
                tenant => string.Equals(tenant.HostName, withoutPort, StringComparison.OrdinalIgnoreCase));
        }

        public TenantInfo? FindByCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            if (string.Equals(code, TenantInfo.DEFAULT_CODE, StringComparison.OrdinalIgnoreCase))
            {
                return Default;
            }

            return _tenants.FirstOrDefault(tenant => string.Equals(tenant.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<TenantInfo> All()
        {
            return _tenants;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            _tenants = await LoadAsync(cancellationToken);
        }

        /// <summary>Reads every registered realm (active or not) with its currently valid modules.</summary>
        protected abstract Task<List<TenantInfo>> LoadAsync(CancellationToken cancellationToken);
    }
}
