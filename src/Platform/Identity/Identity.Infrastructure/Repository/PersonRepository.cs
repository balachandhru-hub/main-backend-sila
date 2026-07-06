using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;
 
namespace Repository
{
    /// <summary>
    /// Class <c>RoleRepository</c> used to implement the methods related for User.
    /// </summary>
    public class PersonRepository : RepositoryBase<Person>, IPersonRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public PersonRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
 
        }
    }
}