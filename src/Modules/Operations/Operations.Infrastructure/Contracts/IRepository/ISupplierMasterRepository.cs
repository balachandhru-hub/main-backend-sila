using Operations.Domain.Enums;
using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface ISupplierMasterRepository : IRepositoryBase<SupplierMaster>
    {
        Task<List<SupplierMaster>> SearchAsync(Guid organizationId, string? entityCode, string? term, StatusKind? status, int take, CancellationToken cancellationToken);
        Task<SupplierMaster?> FindMatchAsync(Guid organizationId, string? taxNumber, string? normalizedName, CancellationToken cancellationToken);
        Task<List<SupplierMaster>> FindCandidatesAsync(Guid organizationId, string? taxNumber, string normalizedName, int take, CancellationToken cancellationToken);
    }
}
