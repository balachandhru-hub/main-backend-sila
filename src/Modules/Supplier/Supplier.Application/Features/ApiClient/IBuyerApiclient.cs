using Supplier.Domain.Dto;

namespace Supplier.Application.Contracts
{
    public interface IBuyerApiClient
    {
        Task<List<Guid>> GetVerifiedSuppliers(
            GetVerifiedSupplierRequestDto request,

            CancellationToken cancellationToken = default);
        Task<GetRFQAttachmentsDto> GetRFQAttachments(
    Guid rfqId,
    CancellationToken cancellationToken = default);
        Task<SupplierVerificationRequestDetailDto> GetSupplierVerificationRequestDetail(
        Guid requestId,
        CancellationToken cancellationToken = default);
        Task UpdateVerificationRequestStatus(
        Guid verificationRequestId,
        string status,
        CancellationToken cancellationToken = default);

        Task<List<RFQQuestionResponseDto>> GetRFQQuestions(
        Guid rfqId,
        CancellationToken cancellationToken = default);
        Task<CostCenterDto> GetCostCenterById(
        Guid costCenterId,
        CancellationToken cancellationToken = default);
        Task StoreQuotationAuditAsync(
            QuotationAuditDto audit,
            CancellationToken cancellationToken);

        Task NotifySupplierRegistrationAsync(
            Guid externalSupplierId,
            Guid rfqId,
            CancellationToken cancellationToken = default);

        Task<Guid?> GetExternalSupplierIdByEmailAsync(
            string email,
            CancellationToken cancellationToken = default);
        Task<CostCenterDto> GetExternalCostCenterById(
Guid costCenterId,
Guid rfqId,
CancellationToken cancellationToken = default);
        Task<List<RFQQuestionResponseDto>> GetExternalRFQQuestions(
        Guid rfqId,
        CancellationToken cancellationToken = default);
        Task<GetRFQAttachmentsDto> GetExternalRFQAttachments(
        Guid rfqId,
        CancellationToken cancellationToken = default);

    }

}