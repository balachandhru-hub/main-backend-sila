using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class SupplierMaterialRepository : RepositoryBase<SupplierMaterial>, ISupplierMaterialRepository
    {
        public SupplierMaterialRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
