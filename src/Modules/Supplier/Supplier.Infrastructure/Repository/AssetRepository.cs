using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.DbContext;
using Supplier.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using SharedKernel.LoggerServices;

namespace Supplier.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>AssetRepository</c> used to implement the methods related for Asset.
    /// </summary>
    public class AssetRepository : RepositoryBase<Asset>, IAssetRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>

        private readonly IConfiguration _configuration;

        private readonly string _dbConnectionString;

        private readonly ILoggerManager _logger;

        public AssetRepository(RepositoryContext repositoryContext, IConfiguration configuration, ILoggerManager logger, string dbConnectionString) : base(repositoryContext)
        {

            _configuration = configuration;
            _logger = logger;
            _dbConnectionString = dbConnectionString;
        }

    }
}