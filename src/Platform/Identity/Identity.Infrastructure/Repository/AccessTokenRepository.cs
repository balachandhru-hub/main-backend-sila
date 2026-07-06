using Contracts.IRepository;
using Identity.Domain.Entities;
using Identity.Infrastructure.DbContext;

namespace Repository
{
    /// <summary>
    /// Class <c>AccessTokenRepository</c> used to implement the methods related for AccessToken.
    /// </summary>
    public class AccessTokenRepository : RepositoryBase<AccessToken>, IAccessTokenRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public AccessTokenRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
