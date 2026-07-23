using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>BuyerSupplierMappingRepository</c> used to implement the methods related for BuyerSupplierMapping.
    /// </summary>
    public class BuyerSupplierMappingRepository : RepositoryBase<BuyerSupplierMapping>, IBuyerSupplierMappingRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public BuyerSupplierMappingRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
