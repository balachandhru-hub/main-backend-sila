using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;
 
namespace Repository
{
    
    public class ModelMappingRepository : RepositoryBase<ModelMapping>, IModelMappingRepository
    {
       
        public ModelMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
 
        }
    }
}