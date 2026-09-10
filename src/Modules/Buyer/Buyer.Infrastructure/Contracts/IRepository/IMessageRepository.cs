using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IMessageRepository
        : IRepositoryBase<Message>
    {
    }
}
