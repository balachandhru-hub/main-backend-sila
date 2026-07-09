using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>BuyerRegistrationRepository</c> used to implement the methods related for BuyerRegistration.
    /// </summary>
    public class BuyerRegistrationRepository : RepositoryBase<BuyerRegistration>, IBuyerRegistrationRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public BuyerRegistrationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
