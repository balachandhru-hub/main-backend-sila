using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class MessageThreadRepository
        : RepositoryBase<MessageThread>,
          IMessageThreadRepository
    {
         public MessageThreadRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
