using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>AssetRepository</c> used to implement the methods related for Asset.
    /// </summary>
    public class AssetRepository : RepositoryBase<Asset>, IAssetRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public AssetRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
