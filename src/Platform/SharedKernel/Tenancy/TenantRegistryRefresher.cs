using Microsoft.Extensions.Hosting;
using SharedKernel.LoggerServices;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// Keeps the in-memory realm registry current. A realm added, changed or disabled in the platform
    /// portal reaches every service within one interval.
    /// </summary>
    public class TenantRegistryRefresher : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly ITenantRegistry _registry;
        private readonly ILoggerManager _logger;

        public TenantRegistryRefresher(ITenantRegistry registry, ILoggerManager logger)
        {
            _registry = registry;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _registry.RefreshAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    // Identity may still be starting. The last known registry stays in use until the next attempt.
                    _logger.LogError($"Realm registry could not be refreshed. {exception.Message}");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }
}
