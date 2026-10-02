using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public sealed record InvoiceProcessingJob(Session Session, Guid InvoiceId, Guid TenantId, Guid EnvironmentId);

public sealed class InvoiceProcessingQueue
{
    private readonly Channel<InvoiceProcessingJob> channel = Channel.CreateUnbounded<InvoiceProcessingJob>();

    public ValueTask EnqueueAsync(InvoiceProcessingJob job, CancellationToken cancellationToken = default) =>
        channel.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<InvoiceProcessingJob> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class InvoiceProcessingWorker(
    InvoiceProcessingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                if (job.TenantId == Guid.Empty || job.EnvironmentId == Guid.Empty)
                {
                    throw new InvalidOperationException("Invoice processing jobs must carry TenantId and EnvironmentId.");
                }

                var accessor = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
                var platform = scope.ServiceProvider.GetRequiredService<SilaMe.Api.Data.PlatformDbContext>();
                var environment = await platform.TenantEnvironments.Include(item => item.Tenant)
                    .SingleAsync(item => item.Id == job.EnvironmentId && item.TenantId == job.TenantId, stoppingToken);
                var tenant = environment.Tenant;
                accessor.Current = new TenantContext(
                    tenant.Id, tenant.TenantCode, tenant.CustomerName, tenant.Status,
                    environment.Id, environment.EnvironmentType, environment.Status, "worker",
                    environment.DatabaseSecretReference, environment.BaseUrl, environment.DataRegion, environment.RouteSlug);
                var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
                var invoice = await operations.GetInvoiceEntityAsync(job.InvoiceId, stoppingToken);
                await operations.RunAdvancedInvoiceExtractionAsync(job.Session, invoice.DocumentId, ExtractionTrigger.INITIAL_BACKEND, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Invoice processing failed for {InvoiceId} Tenant={TenantId}", job.InvoiceId, job.TenantId);
            }
        }
    }
}