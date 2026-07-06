using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Infrastructure.Contracts.IRepository;

public interface IRepositoryWrapper
{
    IUnspscRepository Unspsc { get; }

    IApiConfigRepository ApiConfig { get; }
    IEmailContentRepository EmailContent { get; }
    IEmailSentDetailRepository EmailSentDetail { get; }
    IEmailFailedDetailRepository EmailFailedDetail { get; }
    IEmailCCListRepository EmailCCList { get; }
    

    bool Save();
    Task<bool> SaveAsync();
}