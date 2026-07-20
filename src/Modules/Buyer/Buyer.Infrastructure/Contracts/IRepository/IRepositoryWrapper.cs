namespace Buyer.Infrastructure.Contracts.IRepository
{
    /// <summary>
    /// Repository Wrapper class holding every instance of repository.
    /// </summary>
    public interface IRepositoryWrapper
    {
        IBuyerBusinessProfileRepository BuyerBusinessProfile { get; }
        IBuyerBankAccountRepository BuyerBankAccount { get; }
        IBuyerDeliveryLocationRepository BuyerDeliveryLocation { get; }
        IBuyerRegistrationRepository BuyerRegistration { get; }
        IBuyerCategoryRepository BuyerCategory { get; }
        IItemBuyerMasterRepository ItemBuyerMaster { get; }
        IItemBuyerMasterBulkRepository ItemBuyerMasterBulk { get; }
        
        IAssetRepository Asset { get; }

        bool Save();
        Task<bool> SaveAsync();
    }
}