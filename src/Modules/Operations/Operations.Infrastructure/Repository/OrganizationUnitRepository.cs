using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class OrganizationUnitRepository : RepositoryBase<OrganizationUnit>, IOrganizationUnitRepository
    {
        public OrganizationUnitRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }
    }
}
