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
        Task CheckRFQUserAccessAsync(
            Guid rfqId,
            CancellationToken cancellationToken = default);

        Task<MessageResponseDto> SendMessage(
            SendMessageDto message,
            CancellationToken cancellationToken = default);

        Task<List<MessageThreadSummaryDto>> GetMessageThreads(
            Guid rfqId,
            CancellationToken cancellationToken = default);

        Task<List<MessageResponseDto>> GetMessageHistory(
            Guid threadId,
            int index,
            int limit,
            CancellationToken cancellationToken = default);

        Task MarkThreadRead(
            Guid threadId,
            CancellationToken cancellationToken = default);

        Task<MessageAttachmentFileDto> DownloadMessageAttachment(
            Guid attachmentId,
            CancellationToken cancellationToken = default);
    }

}