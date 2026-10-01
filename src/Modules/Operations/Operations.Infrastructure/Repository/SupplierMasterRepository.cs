using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class SupplierMasterRepository : RepositoryBase<SupplierMaster>, ISupplierMasterRepository
    {
        public SupplierMasterRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<SupplierMaster>> SearchAsync(Guid organizationId, string? entityCode, string? term, StatusKind? status, int take, CancellationToken cancellationToken)
        {
            IQueryable<SupplierMaster> suppliers = RepositoryContext.SupplierMaster
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);
            if (!string.IsNullOrWhiteSpace(entityCode))
            {
                suppliers = suppliers.Where(x => x.EntityCode == entityCode);
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                suppliers = suppliers.Where(x => x.SupplierCode.Contains(term) || x.Name.Contains(term)
                    || (x.TaxNumber != null && x.TaxNumber.Contains(term))
                    || (x.Trn != null && x.Trn.Contains(term))
                    || (x.SearchName != null && x.SearchName.Contains(term))
                    || RepositoryContext.SupplierAlias.Any(alias => alias.SupplierId == x.Id && alias.Alias.Contains(term)));
            }

            if (status != null)
            {
                suppliers = suppliers.Where(x => x.Status == status);
            }

            return suppliers.OrderBy(x => x.Name).Take(take).ToListAsync(cancellationToken);
        }

        // Tax number first, then the normalized name or a confirmed alias. Blocked and deleted suppliers never match.
        public async Task<SupplierMaster?> FindMatchAsync(Guid organizationId, string? taxNumber, string? normalizedName, CancellationToken cancellationToken)
        {
            SupplierMaster? supplier = null;
            if (!string.IsNullOrWhiteSpace(taxNumber))
            {
                supplier = await RepositoryContext.SupplierMaster
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && !x.IsBlocked && !x.IsDeleted && x.TaxNumber == taxNumber, cancellationToken);
            }

            if (supplier == null && !string.IsNullOrWhiteSpace(normalizedName))
            {
                supplier = await RepositoryContext.SupplierMaster
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && !x.IsBlocked && !x.IsDeleted
                        && (x.NormalizedName == normalizedName
                            || RepositoryContext.SupplierAlias.Any(alias => alias.SupplierId == x.Id && alias.NormalizedAlias == normalizedName)), cancellationToken);
            }

            return supplier;
        }

        public Task<List<SupplierMaster>> FindCandidatesAsync(Guid organizationId, string? taxNumber, string normalizedName, int take, CancellationToken cancellationToken)
        {
            return RepositoryContext.SupplierMaster
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Status == StatusKind.ACTIVE && !x.IsBlocked && !x.IsDeleted
                    && ((taxNumber != null && x.TaxNumber == taxNumber)
                        || (normalizedName != "" && (x.NormalizedName == normalizedName
                            || RepositoryContext.SupplierAlias.Any(alias => alias.SupplierId == x.Id && alias.NormalizedAlias == normalizedName)))))
                .OrderBy(x => x.Name)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
    }
}
