using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;

namespace Supplier.Infrastructure.Repository
{
    public class CatalogAssetMappingRepository
        : RepositoryBase<CatalogAssetMapping>,
          ICatalogAssetMappingRepository
    {
        public CatalogAssetMappingRepository(
            RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}