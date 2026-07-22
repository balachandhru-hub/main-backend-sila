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
        IAssetRepository Asset { get; }
        IBuyerDepartmentRepository BuyerDepartment{get;}
        IBuyerCostCenterRepository BuyerCostCenter{get;}

        IRFQRepository RFQ { get; }
        IRFQAttachmentMappingRepository RFQAttachmentMapping { get; }
        IRFQQuestionRepository RFQQuestion { get; }

        IRFQQuestionOptionRepository RFQQuestionOption { get; }
        IRFQItemRepository RFQItem { get; }

        IRFQItemAttachmentMappingRepository RFQItemAttachmentMapping { get; }
        IBuyerSupplierMappingRepository BuyerSupplierMapping{get;}
        bool Save();
        Task<bool> SaveAsync();
    }
}