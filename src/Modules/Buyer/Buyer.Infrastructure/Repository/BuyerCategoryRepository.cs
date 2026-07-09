using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>BuyerCategoryRepository</c> used to implement the methods related for BuyerCategory.
    /// </summary>
    public class BuyerCategoryRepository : RepositoryBase<BuyerCategory>, IBuyerCategoryRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public BuyerCategoryRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
