using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class MaterialRepository : RepositoryBase<Material>, IMaterialRepository
    {
        public MaterialRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
