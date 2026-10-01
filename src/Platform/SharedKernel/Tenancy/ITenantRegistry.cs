namespace SharedKernel.Tenancy
{
    /// <summary>
    /// The customer realms known to this deployment. Identity owns the registry; the other services
    /// keep a copy that is refreshed in the background, so lookups never wait on the network.
    /// </summary>
    public interface ITenantRegistry
    {
        /// <summary>The deployment's own realm, used when a request matches no registered host.</summary>
        TenantInfo Default { get; }

        /// <summary>Finds a realm by the host the browser opened. Returns null when no realm has that host.</summary>
        TenantInfo? FindByHost(string? host);

        TenantInfo? FindByCode(string? code);

        IReadOnlyList<TenantInfo> All();

        /// <summary>Reloads the registry from its source.</summary>
        Task RefreshAsync(CancellationToken cancellationToken);
    }
}
