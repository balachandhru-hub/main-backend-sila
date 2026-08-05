using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>VerficationTemplateQuestionRepository</c> used to implement the methods related for VerficationTemplateQuestion.
    /// </summary>
    public class DefaultVerificationTemplateRepository : RepositoryBase<DefaultVerificationTemplate>, IDefaultVerificationTemplateRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public DefaultVerificationTemplateRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
