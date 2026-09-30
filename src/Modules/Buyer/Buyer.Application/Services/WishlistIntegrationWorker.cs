using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services
{
    /// <summary>
    /// Quartz is referenced by the solution but ConfigureScheduler is empty.
    /// This hosted service is the existing ASP.NET background worker, not a new job framework.
    /// External ERP calls run here so the wishlist HTTP request is not held open.
    /// </summary>
    public class WishlistIntegrationWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public WishlistIntegrationWorker(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IWishlistIntegrationProcessor processor = scope.ServiceProvider.GetRequiredService<IWishlistIntegrationProcessor>();
                    await processor.ProcessAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError($"Wishlist integration worker loop failed. Error={ex.Message}");
                }

                int seconds = 15;
                string? configured = _configuration["WishlistIntegration:PollSeconds"];
                if (int.TryParse(configured, out int parsed) && parsed > 0)
                {
                    seconds = parsed;
                }

                await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
            }
        }
    }
}
