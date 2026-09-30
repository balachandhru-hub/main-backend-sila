using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
namespace Buyer.Infrastructure.Repository
{
    public class MessageRepository
        : RepositoryBase<Message>,
          IMessageRepository
    {
         public MessageRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
