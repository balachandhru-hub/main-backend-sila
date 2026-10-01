using Operations.Domain.Entities;

namespace Operations.Infrastructure.Contracts.IRepository
{
    public interface IIdempotencyRecordRepository : IRepositoryBase<IdempotencyRecord>
    {
    }
}
