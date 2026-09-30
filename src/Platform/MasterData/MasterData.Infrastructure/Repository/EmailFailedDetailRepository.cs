using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class EmailFailedDetailRepository
    : RepositoryBase<EmailFailedDetail>,
      IEmailFailedDetailRepository
{
    public EmailFailedDetailRepository(
        RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}