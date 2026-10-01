using Operations.Domain.Enums;
using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IDocumentTransferJobRepository : IRepositoryBase<DocumentTransferJob>
    {
        Task<List<DocumentTransferJob>> ListDueAsync(DateTime now, int take, CancellationToken cancellationToken);
        Task<DocumentTransferJob?> GetTrackedAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
