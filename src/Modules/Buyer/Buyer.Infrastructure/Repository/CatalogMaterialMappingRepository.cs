using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class CatalogMaterialMappingRepository : RepositoryBase<CatalogMaterialMapping>, ICatalogMaterialMappingRepository
    {
        public CatalogMaterialMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
