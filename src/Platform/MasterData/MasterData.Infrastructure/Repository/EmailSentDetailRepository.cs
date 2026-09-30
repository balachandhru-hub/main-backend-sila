using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class EmailSentDetailRepository
    : RepositoryBase<EmailSentDetail>,
      IEmailSentDetailRepository
{
    public EmailSentDetailRepository(
        RepositoryContext repositoryContext)
        : base(repositoryContext)
    {
    }
}