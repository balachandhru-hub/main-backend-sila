using System.Collections.Concurrent;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// Creates and seeds a service's database for a realm. Each service implements it with its own
    /// migration and seed routine.
    /// </summary>
    public interface ITenantBootstrapper
    {
        /// <param name="scopedProvider">A scope already bound to the realm being prepared.</param>
        void Initialize(IServiceProvider scopedProvider);
    }

    /// <summary>
    /// Makes sure a realm's database is prepared once per process, on the first request for that realm.
    /// </summary>
    public class TenantBootstrapState
    {
        private readonly ConcurrentDictionary<string, object> _locks = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, bool> _ready = new(StringComparer.OrdinalIgnoreCase);

        public void EnsureInitialized(string tenantCode, Action initialize)
        {
            if (_ready.ContainsKey(tenantCode))
            {
                return;
            }

            // Requests for the same realm wait for the first one; a failure leaves the realm
            // unprepared so the next request tries again.
            lock (_locks.GetOrAdd(tenantCode, _ => new object()))
            {
                if (_ready.ContainsKey(tenantCode))
                {
                    return;
                }

                initialize();
                _ready[tenantCode] = true;
            }
        }
    }
}
