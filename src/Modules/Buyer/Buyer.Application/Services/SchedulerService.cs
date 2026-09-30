using Buyer.Application.Services.Integration;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.Contracts.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Services
{
    /// <summary>
    /// Runs wishlist ERP calls after approval. The approve request only stores the decision.
    /// Quartz is referenced by Identity and its ConfigureScheduler method is empty, so this
    /// job is registered from Buyer ConfigureScheduler instead of Program.cs.
    /// </summary>
    public interface ISchedulerService
    {
        Task RunDueIntegrationsAsync(CancellationToken cancellationToken);
    }

    public class SchedulerService : BackgroundService, ISchedulerService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILoggerManager _logger;

        public SchedulerService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILoggerManager logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task RunDueIntegrationsAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IRepositoryWrapper repository = scope.ServiceProvider.GetRequiredService<IRepositoryWrapper>();
            ILoggerManager logger = scope.ServiceProvider.GetRequiredService<ILoggerManager>();
            IHttpClientFactory httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            WishlistIntegrationProcessor processor = new WishlistIntegrationProcessor(
                repository,
                new BuyerPurchaseDocumentGateway(httpClientFactory, logger),
                userContext,
                logger);
            await processor.ProcessAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDueIntegrationsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError($"Wishlist integration job failed. Error={ex.Message}");
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
