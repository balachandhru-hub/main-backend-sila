using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Infrastructure.Contracts.IRepository;

public interface IRepositoryWrapper
{
   
    ISupplierBusinessProfileRepository SupplierBusinessProfile { get; }

    ISupplierRegistrationRepository SupplierRegistration { get; }

    ISupplierBankAccountRepository SupplierBankAccount { get; }

    ISupplierDispatchLocationRepository SupplierDispatchLocation { get; }
    IAssetRepository Asset { get; }
    ISupplierRFQRepository SupplierRFQ { get; }

    ISupplierRFQItemRepository SupplierRFQItem { get; }

    IRFQSupplierMappingRepository RFQSupplierMapping { get; }
    IRFQOrganizationUserMappingRepository RFQOrganizationUserMapping { get; }
    ISupplierQuotationRepository SupplierQuotation { get; }
    ISupplierQuotationItemRepository SupplierQuotationItem { get; }
    ISupplierCatalogRepository SupplierCatalog{get;}

    ICatalogAssetMappingRepository CatalogAssetMapping { get; }
    ISupplierVerificationAnswerRepository SupplierVerificationAnswer { get; }
    ISupplierVerificationAnswerOptionRepository SupplierVerificationAnswerOption { get; }
    IRFQQuestionAnswerRepository RFQQuestionAnswer { get; }
    IRFQQuestionAnswerOptionRepository RFQQuestionAnswerOption { get; }
    ISupplierCategoryRepository SupplierCategory { get; }
    ISupplierEmailVerificationRepository SupplierEmailVerification {get;}
    ISupplierQuotationHistoryRepository SupplierQuotationHistory { get; }
    ISupplierQuotationItemHistoryRepository SupplierQuotationItemHistory { get; }
    IRFQAttachmentMappingRepository RFQAttachmentMapping { get; }

    bool Save();
    Task<bool> SaveAsync();
}