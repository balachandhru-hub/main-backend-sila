using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;
 
namespace Repository
{
    /// <summary>
    /// Class <c>RoleRepository</c> used to implement the methods related for User.
    /// </summary>
    public class UserRepository : RepositoryBase<User>, IUserRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public UserRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
 
        }
    }
}