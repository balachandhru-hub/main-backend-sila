using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.DbContext;


namespace Buyer.Infrastructure.Repository
{
    /// <summary>
    /// Class <c>VerficationTemplateQuestionRepository</c> used to implement the methods related for VerficationTemplateQuestion.
    /// </summary>
    public class VerificationTemplateQuestionOptionRepository : RepositoryBase<VerificationTemplateQuestionOption>, IVerificationTemplateQuestionOptionRepository
    {
        /// <summary>
        /// Constructor for injecting DbContext and ILogger
        /// </summary>
        /// <param name="repositoryContext"></param>
        /// <returns></returns>
        public VerificationTemplateQuestionOptionRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {

        }
    }
}
