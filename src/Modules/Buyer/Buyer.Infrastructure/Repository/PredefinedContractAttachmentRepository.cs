using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class PredefinedContractAttachmentRepository
        : RepositoryBase<PredefinedContractAttachment>, IPredefinedContractAttachmentRepository
    {
        public PredefinedContractAttachmentRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
