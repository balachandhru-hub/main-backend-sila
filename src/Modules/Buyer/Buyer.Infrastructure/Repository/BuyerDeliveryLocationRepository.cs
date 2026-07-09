using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>BuyerDeliveryLocationRepository</c> used to implement the methods related for BuyerDeliveryLocation.
    /// </summary>
    public class BuyerDeliveryLocationRepository : RepositoryBase<BuyerDeliveryLocation>, IBuyerDeliveryLocationRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public BuyerDeliveryLocationRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
