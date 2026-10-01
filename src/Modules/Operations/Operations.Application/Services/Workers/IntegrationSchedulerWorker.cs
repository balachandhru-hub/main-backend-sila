using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Operations.Application.Features.Commands.RunIntegration;
using Operations.Application.Features.Queries.GetDueIntegrations;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services.Workers
{
    /// <summary>
    /// Runs the scheduled ERP pulls (purchase orders, suppliers) every 30 seconds.
    /// </summary>
    public class IntegrationSchedulerWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerManager _logger;

        public IntegrationSchedulerWorker(IServiceScopeFactory scopeFactory, ILoggerManager logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDueIntegrationsInScopeAsync(stoppingToken);
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError($"Integration scheduler cycle failed. Error: {exception}");
                    await Task.Delay(Interval, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// TENANT SCOPE BOUNDARY - the only place this worker touches the database.
        /// One scheduler cycle is dispatched as MediatR requests inside a fresh DI scope (a fresh
        /// DbContext). To make the worker tenant-aware, loop over the tenants around this method
        /// and resolve the tenant here, before the first request is sent.
        /// </summary>
        private async Task RunDueIntegrationsInScopeAsync(CancellationToken stoppingToken)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            List<DueIntegrationDto> due = await mediator.Send(new GetDueIntegrationsQuery(), stoppingToken);
            foreach (DueIntegrationDto item in due)
            {
                try
                {
                    await mediator.Send(new RunIntegrationCommand
                    {
                        OrganizationId = item.OrganizationId,
                        ConfigurationId = item.ConfigurationId,
                        Trigger = IntegrationExecutionTrigger.SCHEDULED,
                        FullSync = false
                    }, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // The run is already recorded as FAILED on the configuration; the next one continues.
                    _logger.LogError($"Scheduled integration failed. ConfigurationId: {item.ConfigurationId}, Error: {exception.Message}");
                }
            }
        }
    }
}
