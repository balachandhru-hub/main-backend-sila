using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;


namespace Repository
{
    /// <summary>
    /// Class <c>UseRoleMappingRepository</c> used to implement the methods related for UseRoleMapping.
    /// </summary>
    public class FeatureRepository : RepositoryBase<Feature>, IFeatureRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public FeatureRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
