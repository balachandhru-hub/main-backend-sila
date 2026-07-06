using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;


namespace Repository
{
    /// <summary>
    /// Class <c>RoleRepository</c> used to implement the methods related for Role.
    /// </summary>
    public class RoleRepository : RepositoryBase<Role>, IRoleRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public RoleRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
