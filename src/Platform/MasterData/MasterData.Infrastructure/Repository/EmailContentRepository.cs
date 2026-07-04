using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class EmailContentRepository 
    : RepositoryBase<EmailContent>, IEmailContentRepository
{
    public EmailContentRepository(RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}