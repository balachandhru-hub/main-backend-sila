using System.Threading.Channels;

namespace Operations.Application.Services.Workers
{
    /// <summary>
    /// An invoice waiting for its backend extraction, with the organization and user that saved it.
    /// </summary>
    public class InvoiceProcessingJob
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
    }

    public interface IInvoiceProcessingQueue
    {
        ValueTask EnqueueAsync(InvoiceProcessingJob job, CancellationToken cancellationToken);

        IAsyncEnumerable<InvoiceProcessingJob> ReadAllAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// In-memory queue between the upload request and the invoice processing worker.
    /// </summary>
    public class InvoiceProcessingQueue : IInvoiceProcessingQueue
    {
        private readonly Channel<InvoiceProcessingJob> _channel = Channel.CreateUnbounded<InvoiceProcessingJob>();

        public ValueTask EnqueueAsync(InvoiceProcessingJob job, CancellationToken cancellationToken)
        {
            return _channel.Writer.WriteAsync(job, cancellationToken);
        }

        public IAsyncEnumerable<InvoiceProcessingJob> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
