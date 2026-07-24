using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class VerificationTemplateRepository
        : RepositoryBase<VerificationTemplate>,
          IVerificationTemplateRepository
    {
         public VerificationTemplateRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {

        }
    }
}