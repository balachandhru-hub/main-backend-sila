using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;


namespace Repository
{
    /// <summary>
    /// Class <c>UseRoleMappingRepository</c> used to implement the methods related for UseRoleMapping.
    /// </summary>
    public class UserRoleMappingRepository : RepositoryBase<UserRoleMapping>, IUserRoleMappingRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public UserRoleMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
