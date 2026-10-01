using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Operations.Application.Features.Commands.ProcessDueDocumentTransfers;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services.Workers
{
    /// <summary>
    /// Copies saved documents to their external destination (SharePoint) every 10 seconds.
    /// </summary>
    public class DocumentTransferWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerManager _logger;

        public DocumentTransferWorker(IServiceScopeFactory scopeFactory, ILoggerManager logger)
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
                    await ProcessDueTransfersInScopeAsync(stoppingToken);
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError($"Document transfer worker cycle failed. Error: {exception}");
                    await Task.Delay(Interval, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// TENANT SCOPE BOUNDARY - the only place this worker touches the database.
        /// One polling cycle is dispatched as a MediatR command inside a fresh DI scope (a fresh
        /// DbContext). To make the worker tenant-aware, loop over the tenants around this method
        /// and resolve the tenant here, before the command is sent.
        /// </summary>
        private async Task ProcessDueTransfersInScopeAsync(CancellationToken stoppingToken)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new ProcessDueDocumentTransfersCommand(), stoppingToken);
        }
    }
}
