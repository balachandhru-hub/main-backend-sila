using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Operations.Application.Features.Commands.RunDocumentAdvancedExtraction;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IServices;
using SharedKernel.LoggerServices;

namespace Operations.Application.Services.Workers
{
    /// <summary>
    /// Runs the backend extraction of invoices saved with "defer full extraction".
    /// </summary>
    public class InvoiceProcessingWorker : BackgroundService
    {
        private readonly IInvoiceProcessingQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILoggerManager _logger;

        public InvoiceProcessingWorker(IInvoiceProcessingQueue queue, IServiceScopeFactory scopeFactory, ILoggerManager logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (InvoiceProcessingJob job in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessJobInScopeAsync(job, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError($"Invoice processing failed. DocumentId: {job.DocumentId}, OrganizationId: {job.OrganizationId}, Error: {exception}");
                }
            }
        }

        /// <summary>
        /// TENANT SCOPE BOUNDARY - the only place this worker touches the database.
        /// Every job is dispatched as a MediatR command inside a fresh DI scope (a fresh DbContext).
        /// The job carries the organization of the user who queued it (job.OrganizationId): resolve
        /// the tenant from it here, before the command is sent.
        /// </summary>
        private async Task ProcessJobInScopeAsync(InvoiceProcessingJob job, CancellationToken stoppingToken)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IUserContext>().SetCurrentUserId(job.UserId);
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new RunDocumentAdvancedExtractionCommand
            {
                OrganizationId = job.OrganizationId,
                UserId = job.UserId,
                DocumentId = job.DocumentId,
                Trigger = ExtractionTrigger.INITIAL_BACKEND
            }, stoppingToken);
        }
    }
}
