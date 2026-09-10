using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class MessageAttachmentRepository
        : RepositoryBase<MessageAttachment>,
          IMessageAttachmentRepository
    {
         public MessageAttachmentRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
