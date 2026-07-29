using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;
 
namespace Repository
{
    
    public class OrganizationModelMappingRepository : RepositoryBase<OrganizationModelMapping>, IOrganizationModelMappingRepository
    {
       
        public OrganizationModelMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
 
        }
    }
}