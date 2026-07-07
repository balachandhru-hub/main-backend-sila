using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;


namespace Repository
{
    /// <summary>
    /// Class <c>UseRoleMappingRepository</c> used to implement the methods related for UseRoleMapping.
    /// </summary>
    public class RoleFeatureMappingRepository : RepositoryBase<RoleFeatureMapping>, IRoleFeatureMappingRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public RoleFeatureMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
