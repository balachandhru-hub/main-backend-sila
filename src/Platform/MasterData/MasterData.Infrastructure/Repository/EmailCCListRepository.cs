using MasterData.Domain.Entities;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;

namespace MasterData.Infrastructure.Repository;

public class EmailCCListRepository :
    RepositoryBase<EmailCCList>,
    IEmailCCListRepository
{
    public EmailCCListRepository(
        RepositoryContext repositoryContext)
        : base(repositoryContext)
    {

    }
}