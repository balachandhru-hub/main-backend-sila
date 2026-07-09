using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>BuyerBankAccountRepository</c> used to implement the methods related for BuyerBankAccount.
    /// </summary>
    public class BuyerBankAccountRepository : RepositoryBase<BuyerBankAccount>, IBuyerBankAccountRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public BuyerBankAccountRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
