using MasterData.Infrastructure.Contracts.IRepository;

namespace MasterData.Infrastructure.Contracts.IRepository;

public interface IRepositoryWrapper
{
    IUnspscRepository Unspsc { get; }

    IApiConfigRepository ApiConfig { get; }
    IMetadataRepository Metadata { get; }
    IEmailContentRepository EmailContent { get; }
    IEmailSentDetailRepository EmailSentDetail { get; }
    IEmailFailedDetailRepository EmailFailedDetail { get; }
    IEmailCCListRepository EmailCCList { get; }
    ICountryListRepository CountryList { get; }
    ICurrencyRepository Currency { get; }
    IUnitRepository Unit { get; }

    bool Save();
    Task<bool> SaveAsync();
}