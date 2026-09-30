using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;

namespace Buyer.Infrastructure.Repository
{
    public class ContractAttachmentRepository : RepositoryBase<ContractAttachment>, IContractAttachmentRepository
    {
        public ContractAttachmentRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
