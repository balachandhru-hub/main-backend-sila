using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IDocumentRepository : IRepositoryBase<Document>
    {
        Task<Document?> GetTrackedAsync(Guid documentId, Guid organizationId, CancellationToken cancellationToken);
    }
}
