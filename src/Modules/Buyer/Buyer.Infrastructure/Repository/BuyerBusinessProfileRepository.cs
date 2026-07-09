using Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Repository
{
    /// <summary>
    /// Class <c>BuyerBusinessProfileRepository</c> used to implement the methods related for BuyerBusinessProfile.
    /// </summary>
    public class BuyerBusinessProfileRepository : RepositoryBase<BuyerBusinessProfile>, IBuyerBusinessProfileRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public BuyerBusinessProfileRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
